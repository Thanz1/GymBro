[CmdletBinding()]
param(
    [ValidateSet("single", "multi", "all")]
    [string]$Profile = "all",

    [string]$ConfigPath = "",

    [string]$ComposeFile = "",

    [switch]$BuildCompose,

    [switch]$StartCompose,

    [switch]$RunScanner,

    [switch]$NoReport,

    [switch]$FailOnFinding
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ConfigPath)) {
    $ConfigPath = Join-Path $PSScriptRoot "docker-attack-targets.json"
}

if ([string]::IsNullOrWhiteSpace($ComposeFile)) {
    $repoRoot = Join-Path $PSScriptRoot ".."
    if ($Profile -eq "single") {
        $ComposeFile = Join-Path $repoRoot "docker-compose-single.yml"
    }
    else {
        # Multi-stage dùng docker-compose-multistage.yml
        $ComposeFile = Join-Path $repoRoot "docker-compose-multistage.yml"
    }
}

$Results = New-Object System.Collections.Generic.List[object]
$SecretPattern = "(?i)(password=|mssql_sa_password|jwt__key|jwt:key|smtp.*password|rabbitmq_default_pass|apikey|api_key|token=|secret=|GymBro@2024Password|ChangeMe_)"

function Invoke-External {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,

        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        $output = & $FilePath @Arguments 2>&1
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }

    [pscustomobject]@{
        ExitCode = $exitCode
        Output   = (($output | ForEach-Object { $_.ToString() }) -join "`n")
    }
}

function Add-TestResult {
    param(
        [string]$Target,
        [string]$Case,
        [ValidateSet("PASS", "FAIL", "WARN", "SKIP", "INFO")]
        [string]$Status,
        [string]$Evidence,
        [string]$Recommendation,
        [ValidateSet("Info", "Low", "Medium", "High")]
        [string]$Severity = "Medium"
    )

    $Results.Add([pscustomobject]@{
        Target         = $Target
        Case           = $Case
        Severity       = $Severity
        Status         = $Status
        Evidence       = (($Evidence -replace "`r?`n", " | ").Trim())
        Recommendation = $Recommendation
    })
}

function Redact-Secrets {
    param([string]$Text)

    $Text `
        -replace "(?i)(Password=)[^;\s]+", '$1***' `
        -replace "(?i)(MSSQL_SA_PASSWORD=).*", '$1***' `
        -replace "(?i)(MSSQL_SA_PASSWORD:\s*).*", '$1***' `
        -replace "(?i)(RABBITMQ_DEFAULT_PASS[:=]\s*).*", '$1***' `
        -replace "(?i)(Jwt__Key[:=]\s*).*", '$1***' `
        -replace "(?i)(Jwt:Key[:=]\s*).*", '$1***'
}

function Escape-MarkdownCell {
    param([string]$Text)

    if ($null -eq $Text) {
        return ""
    }

    ($Text -replace "\|", "\|" -replace "`r?`n", "<br>")
}

function Test-DockerAvailable {
    $cmd = Get-Command docker -ErrorAction SilentlyContinue
    if (-not $cmd) {
        throw "Docker CLI was not found in PATH."
    }

    $version = Invoke-External -FilePath "docker" -Arguments @("version", "--format", "{{.Server.Version}}")
    if ($version.ExitCode -ne 0) {
        throw "Docker daemon is not ready: $($version.Output)"
    }
}

function Test-ImageExists {
    param([string]$Image)

    if ([string]::IsNullOrWhiteSpace($Image)) {
        return $false
    }

    $result = Invoke-External -FilePath "docker" -Arguments @("image", "inspect", $Image)
    $result.ExitCode -eq 0
}

function Test-ContainerExists {
    param([string]$Container)

    if ([string]::IsNullOrWhiteSpace($Container)) {
        return $false
    }

    $result = Invoke-External -FilePath "docker" -Arguments @("container", "inspect", $Container)
    $result.ExitCode -eq 0
}

function Invoke-ImageShell {
    param(
        [string]$Image,
        [string]$Command
    )

    Invoke-External -FilePath "docker" -Arguments @("run", "--rm", "--entrypoint", "/bin/sh", $Image, "-c", $Command)
}

function Get-ImageSizeText {
    param([string]$Image)

    $result = Invoke-External -FilePath "docker" -Arguments @("image", "inspect", $Image, "--format", "{{.Size}}")
    if ($result.ExitCode -ne 0) {
        return "unknown"
    }

    try {
        $bytes = [double]($result.Output.Trim())
        return ("{0:N1} MB" -f ($bytes / 1MB))
    }
    catch {
        return $result.Output.Trim()
    }
}

# ========================================================================
# TẤN CÔNG 1: Test-StaticImage — Quét nội dung image tĩnh
# (không cần container chạy). Kiểm tra: non-root user, leak SDK,
# leak source code, build tools, secrets trong history/env, HEALTHCHECK.
# ========================================================================
function Test-StaticImage {
    param($Target)

    $name = [string]$Target.name
    $image = [string]$Target.image

    if (-not (Test-ImageExists -Image $image)) {
        Add-TestResult -Target $name -Case "IMG-00 image exists" -Status "SKIP" -Severity "Info" -Evidence "Image not found: $image" -Recommendation "Build the image first with docker compose build or docker build."
        return
    }

    Add-TestResult -Target $name -Case "IMG-00 image size" -Status "INFO" -Severity "Info" -Evidence "$image = $(Get-ImageSizeText -Image $image)" -Recommendation "Use this value to compare single-stage and secure multi-stage images."

    # IMG-01: Non-root user
    $userResult = Invoke-ImageShell -Image $image -Command 'id -u; id -un 2>/dev/null'
    if ($userResult.ExitCode -ne 0) {
        Add-TestResult -Target $name -Case "IMG-01 runtime user is non-root" -Status "WARN" -Severity "Medium" -Evidence $userResult.Output -Recommendation "Could not read runtime user. Check image shell support."
    }
    else {
        $lines = @($userResult.Output -split "`n" | ForEach-Object { $_.Trim() } | Where-Object { $_ })
        $uid = if ($lines.Count -gt 0) { $lines[0] } else { "" }
        $uname = if ($lines.Count -gt 1) { $lines[1] } else { "" }
        if ($uid -eq "0" -or [string]::IsNullOrWhiteSpace($uid)) {
            Add-TestResult -Target $name -Case "IMG-01 runtime user is non-root" -Status "FAIL" -Severity "High" -Evidence "uid=$uid user=$uname" -Recommendation "Create a non-root user in final stage and set USER. Multi-stage images should use `"USER gymbro`"."
        }
        else {
            Add-TestResult -Target $name -Case "IMG-01 runtime user is non-root" -Status "PASS" -Severity "High" -Evidence "uid=$uid user=$uname" -Recommendation "Least privilege is enabled at image level. Multi-stage hardened with non-root user."
        }
    }

    # IMG-02: No .NET SDK
    $sdkResult = Invoke-ImageShell -Image $image -Command 'dotnet --list-sdks 2>/dev/null'
    if ([string]::IsNullOrWhiteSpace($sdkResult.Output)) {
        Add-TestResult -Target $name -Case "IMG-02 no .NET SDK in runtime" -Status "PASS" -Severity "High" -Evidence "dotnet --list-sdks returned no SDK" -Recommendation "Multi-stage build separates SDK (build) from aspnet (runtime). No SDK = smaller attack surface."
    }
    else {
        Add-TestResult -Target $name -Case "IMG-02 no .NET SDK in runtime" -Status "FAIL" -Severity "High" -Evidence $sdkResult.Output -Recommendation "Final image should use mcr.microsoft.com/dotnet/aspnet, not dotnet/sdk. Single-stage images fail this test."
    }

    # IMG-03: No source code leakage
    $sourceCommand = "find /app /src -type f \( -name '*.cs' -o -name '*.csproj' -o -name '*.sln' \) 2>/dev/null | head -n 20"
    $sourceResult = Invoke-ImageShell -Image $image -Command $sourceCommand
    if ([string]::IsNullOrWhiteSpace($sourceResult.Output)) {
        Add-TestResult -Target $name -Case "IMG-03 no source/build files leaked" -Status "PASS" -Severity "High" -Evidence "No .cs/.csproj/.sln files found under /app or /src" -Recommendation "Only published artifacts are copied to final image. Multi-stage prevents source code leakage."
    }
    else {
        Add-TestResult -Target $name -Case "IMG-03 no source/build files leaked" -Status "FAIL" -Severity "High" -Evidence $sourceResult.Output -Recommendation "Do not copy the whole source tree into the final image. Use multi-stage COPY --from=build."
    }

    # IMG-04: No debug/development files
    $debugCommand = "find /app -type f \( -name '*.pdb' -o -name 'appsettings.Development.json' \) 2>/dev/null | head -n 20"
    $debugResult = Invoke-ImageShell -Image $image -Command $debugCommand
    if ([string]::IsNullOrWhiteSpace($debugResult.Output)) {
        Add-TestResult -Target $name -Case "IMG-04 no debug/development files" -Status "PASS" -Severity "Medium" -Evidence "No .pdb or appsettings.Development.json found under /app" -Recommendation "Keep DebugSymbols=false and remove development config from final image."
    }
    else {
        Add-TestResult -Target $name -Case "IMG-04 no debug/development files" -Status "WARN" -Severity "Medium" -Evidence $debugResult.Output -Recommendation "Consider removing .pdb and appsettings.Development.json from runtime image."
    }

    # IMG-05: No build tools
    $toolCommand = 'command -v dotnet-ef 2>/dev/null; command -v gcc 2>/dev/null; command -v make 2>/dev/null; command -v git 2>/dev/null'
    $toolResult = Invoke-ImageShell -Image $image -Command $toolCommand
    if ([string]::IsNullOrWhiteSpace($toolResult.Output)) {
        Add-TestResult -Target $name -Case "IMG-05 no common build tools" -Status "PASS" -Severity "Medium" -Evidence "dotnet-ef/gcc/make/git were not found" -Recommendation "Multi-stage separates build tools (SDK image) from runtime (aspnet image)."
    }
    else {
        Add-TestResult -Target $name -Case "IMG-05 no common build tools" -Status "FAIL" -Severity "Medium" -Evidence $toolResult.Output -Recommendation "Remove build tools from final image with multi-stage build. Single-stage SDK images fail this test."
    }

    # IMG-06: Write access test
    $writeCommand = '(touch /app/.attack_write_test 2>/dev/null && rm -f /app/.attack_write_test && echo WRITABLE) || echo BLOCKED'
    $writeResult = Invoke-ImageShell -Image $image -Command $writeCommand
    if ($writeResult.Output -match "BLOCKED") {
        Add-TestResult -Target $name -Case "IMG-06 simulated write to /app" -Status "PASS" -Severity "Medium" -Evidence "Write to /app was blocked (non-root user + chown)" -Recommendation "Multi-stage with non-root user blocks unauthorized writes."
    }
    else {
        Add-TestResult -Target $name -Case "IMG-06 simulated write to /app" -Status "WARN" -Severity "Medium" -Evidence $writeResult.Output -Recommendation "Use non-root USER and chown in Dockerfile to restrict write access."
    }

    # IMG-07: No secrets in image history
    $history = Invoke-External -FilePath "docker" -Arguments @("history", "--no-trunc", $image)
    if ($history.ExitCode -eq 0 -and $history.Output -match $SecretPattern) {
        Add-TestResult -Target $name -Case "IMG-07 no secrets in image history" -Status "FAIL" -Severity "High" -Evidence (Redact-Secrets $history.Output) -Recommendation "Do not put passwords/keys in Dockerfile RUN/ENV/ARG."
    }
    else {
        Add-TestResult -Target $name -Case "IMG-07 no secrets in image history" -Status "PASS" -Severity "High" -Evidence "No secret pattern found in docker history" -Recommendation "Keep secrets out of image layers."
    }

    # IMG-08: No secrets in image env
    $envInspect = Invoke-External -FilePath "docker" -Arguments @("image", "inspect", $image, "--format", "{{json .Config.Env}}")
    if ($envInspect.ExitCode -eq 0 -and $envInspect.Output -match $SecretPattern) {
        Add-TestResult -Target $name -Case "IMG-08 no secrets in image env" -Status "FAIL" -Severity "High" -Evidence (Redact-Secrets $envInspect.Output) -Recommendation "Do not set secrets with ENV in Dockerfile."
    }
    else {
        Add-TestResult -Target $name -Case "IMG-08 no secrets in image env" -Status "PASS" -Severity "High" -Evidence "No secret pattern found in image env" -Recommendation "Pass secrets at runtime through env/secrets manager."
    }

    # IMG-09: HEALTHCHECK present (new test for multi-stage hardened images)
    $healthcheck = Invoke-External -FilePath "docker" -Arguments @("image", "inspect", $image, "--format", "{{json .Config.Healthcheck}}")
    if ($healthcheck.Output -match "null") {
        Add-TestResult -Target $name -Case "IMG-09 HEALTHCHECK defined" -Status "WARN" -Severity "Medium" -Evidence "No HEALTHCHECK found" -Recommendation "Add HEALTHCHECK instruction in Dockerfile for automatic container health monitoring."
    }
    else {
        Add-TestResult -Target $name -Case "IMG-09 HEALTHCHECK defined" -Status "PASS" -Severity "Medium" -Evidence $healthcheck.Output -Recommendation "HEALTHCHECK enables Docker to auto-restart unhealthy containers. Multi-stage hardened includes this."
    }

    # IMG-10: CVE scan (optional)
    if ($RunScanner) {
        $trivy = Get-Command trivy -ErrorAction SilentlyContinue
        if ($trivy) {
            $scan = Invoke-External -FilePath "trivy" -Arguments @("image", "--severity", "CRITICAL,HIGH", "--no-progress", $image)
            $status = if ($scan.Output -match "CRITICAL|HIGH") { "WARN" } else { "PASS" }
            Add-TestResult -Target $name -Case "IMG-10 CVE scan with Trivy" -Status $status -Severity "High" -Evidence $scan.Output -Recommendation "Update base images/packages when High/Critical CVEs exist."
        }
        else {
            $scout = Invoke-External -FilePath "docker" -Arguments @("scout", "version")
            if ($scout.ExitCode -eq 0) {
                $scan = Invoke-External -FilePath "docker" -Arguments @("scout", "cves", $image)
                $status = if ($scan.Output -match "(?i)critical|high") { "WARN" } else { "PASS" }
                Add-TestResult -Target $name -Case "IMG-10 CVE scan with Docker Scout" -Status $status -Severity "High" -Evidence $scan.Output -Recommendation "Update base images/packages when High/Critical CVEs exist."
            }
            else {
                Add-TestResult -Target $name -Case "IMG-10 CVE scan" -Status "SKIP" -Severity "Info" -Evidence "Trivy/Docker Scout not found" -Recommendation "Install Trivy or enable Docker Scout for vulnerability scanning."
            }
        }
    }
}

# ========================================================================
# TẤN CÔNG 2: Test-ContainerRuntime — Quét container đang chạy
# Kiểm tra: non-root user, read-only filesystem, cap_drop,
# no-new-privileges, memory limits, published ports.
# ========================================================================
function Test-ContainerRuntime {
    param($Target, $Profile)

    $name = [string]$Target.name
    $container = [string]$Target.container

    if (-not (Test-ContainerExists -Container $container)) {
        Add-TestResult -Target $name -Case "RUN-00 container exists" -Status "SKIP" -Severity "Info" -Evidence "Container not found: $container" -Recommendation "Run docker compose up -d or use -StartCompose to test runtime hardening."
        return
    }

    # RUN-01: Non-root user
    $user = Invoke-External -FilePath "docker" -Arguments @("inspect", $container, "--format", "{{.Config.User}}")
    $userValue = $user.Output.Trim()
    if ([string]::IsNullOrWhiteSpace($userValue) -or $userValue -eq "0" -or $userValue -eq "root") {
        Add-TestResult -Target $name -Case "RUN-01 container user is non-root" -Status "FAIL" -Severity "High" -Evidence "Config.User='$userValue'" -Recommendation "Set USER non-root in Dockerfile final stage. Single-stage images run as root by default."
    }
    else {
        Add-TestResult -Target $name -Case "RUN-01 container user is non-root" -Status "PASS" -Severity "High" -Evidence "Config.User='$userValue'" -Recommendation "Multi-stage hardened with USER gymbro. Least privilege at runtime."
    }

    # RUN-02: Read-only filesystem
    $readonly = Invoke-External -FilePath "docker" -Arguments @("inspect", $container, "--format", "{{.HostConfig.ReadonlyRootfs}}")
    if ($readonly.Output.Trim() -eq "true") {
        Add-TestResult -Target $name -Case "RUN-02 root filesystem is read-only" -Status "PASS" -Severity "Medium" -Evidence "ReadonlyRootfs=true" -Recommendation "Mount volumes only for writable folders."
    }
    else {
        Add-TestResult -Target $name -Case "RUN-02 root filesystem is read-only" -Status "WARN" -Severity "Medium" -Evidence "ReadonlyRootfs=$($readonly.Output.Trim())" -Recommendation "Consider read_only: true in production Compose. Currently disabled to allow app functionality."
    }

    # RUN-03: Capabilities dropped
    $capDrop = Invoke-External -FilePath "docker" -Arguments @("inspect", $container, "--format", "{{json .HostConfig.CapDrop}}")
    if ($capDrop.Output -match "ALL") {
        Add-TestResult -Target $name -Case "RUN-03 Linux capabilities dropped" -Status "PASS" -Severity "Medium" -Evidence $capDrop.Output -Recommendation "cap_drop: ALL reduces unnecessary kernel privileges. Multi-stage hardened enables this."
    }
    else {
        Add-TestResult -Target $name -Case "RUN-03 Linux capabilities dropped" -Status "WARN" -Severity "Medium" -Evidence $capDrop.Output -Recommendation "Consider cap_drop: ALL. Single-stage images don't drop capabilities."
    }

    # RUN-04: no-new-privileges
    $securityOpt = Invoke-External -FilePath "docker" -Arguments @("inspect", $container, "--format", "{{json .HostConfig.SecurityOpt}}")
    if ($securityOpt.Output -match "no-new-privileges") {
        Add-TestResult -Target $name -Case "RUN-04 no-new-privileges enabled" -Status "PASS" -Severity "Medium" -Evidence $securityOpt.Output -Recommendation "security_opt: no-new-privileges:true prevents privilege escalation. Multi-stage hardened includes this."
    }
    else {
        Add-TestResult -Target $name -Case "RUN-04 no-new-privileges enabled" -Status "WARN" -Severity "Medium" -Evidence $securityOpt.Output -Recommendation "Consider security_opt: no-new-privileges:true. Single-stage images don't have this."
    }

    # RUN-05: Memory limits (new test)
    $memory = Invoke-External -FilePath "docker" -Arguments @("inspect", $container, "--format", "{{.HostConfig.Memory}}")
    $memValue = $memory.Output.Trim()
    if ($memValue -eq "0") {
        Add-TestResult -Target $name -Case "RUN-05 memory limit set" -Status "WARN" -Severity "Low" -Evidence "Memory=unlimited" -Recommendation "Set memory limits via deploy.resources.limits to prevent DoS. Multi-stage hardened includes limits."
    }
    else {
        Add-TestResult -Target $name -Case "RUN-05 memory limit set" -Status "PASS" -Severity "Low" -Evidence "Memory=$([math]::Round($memValue/1MB)) MB" -Recommendation "Memory limits prevent a single container from exhausting host resources."
    }

    # RUN-06: Published ports (info)
    $ports = Invoke-External -FilePath "docker" -Arguments @("inspect", $container, "--format", "{{json .NetworkSettings.Ports}}")
    Add-TestResult -Target $name -Case "RUN-06 published ports" -Status "INFO" -Severity "Info" -Evidence $ports.Output -Recommendation "Production should expose only Web/Gateway when possible."
}

# ========================================================================
# TẤN CÔNG 3: Test-ComposeSecurity — Quét file docker-compose.yml
# Kiểm tra: hard-coded secrets, Development mode,
# over-published ports, security hardening features.
# ========================================================================
function Test-ComposeSecurity {
    param([string]$ProfileName)

    $composeName = Split-Path -Leaf $ComposeFile

    if (-not (Test-Path -LiteralPath $ComposeFile)) {
        Add-TestResult -Target $composeName -Case "CMP-00 compose file exists" -Status "SKIP" -Severity "Info" -Evidence "File not found: $ComposeFile" -Recommendation "Pass the correct -ComposeFile path."
        return
    }

    $raw = Get-Content -Raw -LiteralPath $ComposeFile

    # CMP-01: No hard-coded secrets
    $secretMatches = Select-String -LiteralPath $ComposeFile -Pattern $SecretPattern -AllMatches
    if ($secretMatches) {
        $evidenceItems = @()
        foreach ($m in $secretMatches) {
            $evidenceItems += "line $($m.LineNumber): $(Redact-Secrets $m.Line.Trim())"
        }
        Add-TestResult -Target $composeName -Case "CMP-01 no hard-coded secrets" -Status "FAIL" -Severity "High" -Evidence ($evidenceItems -join " | ") -Recommendation "Move passwords/JWT/SMTP secrets to .env, Docker secrets, or a secret manager."
    }
    else {
        Add-TestResult -Target $composeName -Case "CMP-01 no hard-coded secrets" -Status "PASS" -Severity "High" -Evidence "No secret pattern found in Compose" -Recommendation "Keep secrets outside source code."
    }

    # CMP-02: Environment check
    if ($raw -match "ASPNETCORE_ENVIRONMENT=Development" -or $raw -match "ASPNETCORE_ENVIRONMENT:\s*Development") {
        Add-TestResult -Target $composeName -Case "CMP-02 not Development for secure deploy" -Status "WARN" -Severity "Medium" -Evidence "Compose declares ASPNETCORE_ENVIRONMENT=Development" -Recommendation "Use Production when evaluating deployment security."
    }
    else {
        Add-TestResult -Target $composeName -Case "CMP-02 not Development for secure deploy" -Status "PASS" -Severity "Medium" -Evidence "Development environment was not found" -Recommendation "Keep Production for runtime security tests."
    }

    # CMP-03: Internal ports exposure
    $publicPortPatterns = @(
        "1433:1433", "1434:1433",
        "5672:5672", "5673:5672",
        "15672:15672", "15673:15672",
        "7001:8080", "7002:8080", "7003:8080",
        "7011:8080", "7012:8080", "7013:8080"
    )
    $published = @()
    foreach ($pattern in $publicPortPatterns) {
        if ($raw.Contains($pattern)) {
            $published += $pattern
        }
    }

    if ($published.Count -gt 0) {
        Add-TestResult -Target $composeName -Case "CMP-03 internal ports are not over-published" -Status "WARN" -Severity "Medium" -Evidence ($published -join ", ") -Recommendation "Production should expose only Web/Gateway; keep APIs, SQL Server, and RabbitMQ internal when possible."
    }
    else {
        Add-TestResult -Target $composeName -Case "CMP-03 internal ports are not over-published" -Status "PASS" -Severity "Medium" -Evidence "No common internal public ports found" -Recommendation "Keep network exposure minimal."
    }

    # CMP-04: Security hardening features (new test for multi-stage vs single comparison)
    $hasCapDrop = ($raw -match "cap_drop:")
    $hasNoNewPriv = ($raw -match "no-new-privileges")
    $hasMemLimit = ($raw -match "deploy:" -and $raw -match "resources:" -and $raw -match "limits:" -and $raw -match "memory:")

    $hardeningFeatures = @()
    if ($hasCapDrop) { $hardeningFeatures += "cap_drop" }
    if ($hasNoNewPriv) { $hardeningFeatures += "no-new-privileges" }
    if ($hasMemLimit) { $hardeningFeatures += "memory-limits" }

    if ($hardeningFeatures.Count -gt 0) {
        Add-TestResult -Target $composeName -Case "CMP-04 security hardening in compose" -Status "PASS" -Severity "Medium" -Evidence ($hardeningFeatures -join ", ") -Recommendation "Multi-stage compose includes runtime hardening: $($hardeningFeatures -join ', '). Single-stage compose lacks these protections."
    }
    else {
        Add-TestResult -Target $composeName -Case "CMP-04 security hardening in compose" -Status "FAIL" -Severity "Medium" -Evidence "No cap_drop, no-new-privileges, or memory limits found" -Recommendation "Add security hardening to docker compose for production deployments."
    }
}

# ==============================
# COMPARISON REPORT
# ==============================
# ========================================================================
# BÁO CÁO: Write-ComparisonReport — Tạo file Markdown so sánh
# Single-stage vs Multi-stage hardened, liệt kê từng cải tiến bảo mật.
# ========================================================================
function Write-ComparisonReport {
    $reportDir = Join-Path $PSScriptRoot "reports"
    New-Item -ItemType Directory -Force -Path $reportDir | Out-Null

    $stamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $reportPath = Join-Path $reportDir "security-comparison-$stamp.md"

    # Group results: single vs multi
    $singleResults = @($Results | Where-Object { $_.Target -match "single" })
    $multiResults = @($Results | Where-Object { $_.Target -match "multi" })

    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add("# GymBro Docker Security Comparison Report")
    $lines.Add("")
    $lines.Add("**Time:** $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')")
    $lines.Add("")
    $lines.Add("## Executive Summary")
    $lines.Add("")
    $lines.Add("This report compares Docker security test results between:")
    $lines.Add("- **Single-stage** (baseline): SDK image, no hardening")
    $lines.Add("- **Multi-stage hardened** (secure): aspnet runtime, non-root user, capability drop, no-new-privileges, memory limits, HEALTHCHECK")
    $lines.Add("")

    # Summary table
    $lines.Add("## Summary Comparison")
    $lines.Add("")
    $lines.Add("| Test Case | Single-stage | Multi-stage hardened | Improvement |")
    $lines.Add("|-----------|:-----------:|:-------------------:|-------------|")

    $testCases = @($Results | Where-Object { $_.Target -match "single" -or $_.Target -match "multi" } |
        Group-Object Case |
        Sort-Object Name)

    foreach ($case in $testCases) {
        $caseName = $case.Name
        $single = @($case.Group | Where-Object { $_.Target -match "single" } | Select-Object -First 1)
        $multi = @($case.Group | Where-Object { $_.Target -match "multi" } | Select-Object -First 1)

        $singleStatus = if ($single) { $single.Status } else { "N/A" }
        $multiStatus = if ($multi) { $multi.Status } else { "N/A" }

        # Determine improvement
        $improvement = ""
        if ($singleStatus -eq "FAIL" -and $multiStatus -eq "PASS") { $improvement = "✅ Major improvement" }
        elseif ($singleStatus -eq "WARN" -and $multiStatus -eq "PASS") { $improvement = "✅ Improved" }
        elseif ($singleStatus -eq "FAIL" -and $multiStatus -eq "WARN") { $improvement = "⚠️ Partially improved" }
        elseif ($singleStatus -eq "PASS" -and $multiStatus -eq "PASS") { $improvement = "➡️ Both pass" }
        elseif ($singleStatus -eq $multiStatus) { $improvement = "➡️ Same" }
        else { $improvement = "N/A" }

        $lines.Add("| $caseName | $singleStatus | $multiStatus | $improvement |")
    }

    $lines.Add("")

    # Detailed results
    $lines.Add("## Detailed Results")
    $lines.Add("")
    $lines.Add("| Profile | Target | Case | Severity | Status | Evidence |")
    $lines.Add("| --- | --- | --- | --- | --- | --- |")
    foreach ($r in ($Results | Sort-Object Target, Case)) {
        $profile = if ($r.Target -match "single") { "SINGLE" } else { "MULTI" }
        $lines.Add("| $profile | $(Escape-MarkdownCell $r.Target) | $(Escape-MarkdownCell $r.Case) | $($r.Severity) | $($r.Status) | $(Escape-MarkdownCell $r.Evidence) |")
    }

    $lines.Add("")

    # What multi-stage fixes
    $lines.Add("## What Multi-stage Hardening Adds")
    $lines.Add("")
    $lines.Add("| Feature | Description | Security Benefit |")
    $lines.Add("|---------|-------------|-----------------|")
    $lines.Add("| **Multi-stage build** | Separate SDK (build) from aspnet (runtime) | No SDK, source code, or build tools in runtime image |")
    $lines.Add("| **Non-root user** | `USER gymbro` in Dockerfile | Attacker cannot modify app files even if they gain shell access |")
    $lines.Add("| **Capability drop** | `cap_drop: ALL` + `cap_add: NET_BIND_SERVICE` | Container has minimal kernel capabilities |")
    $lines.Add("| **no-new-privileges** | `security_opt: no-new-privileges:true` | Prevents privilege escalation via setuid binaries |")
    $lines.Add("| **Memory limits** | `deploy.resources.limits.memory` | Prevents one container from exhausting host RAM (DoS protection) |")
    $lines.Add("| **HEALTHCHECK** | `HEALTHCHECK` instruction in Dockerfile | Docker auto-restarts unhealthy containers |")
    $lines.Add("| **Image size reduction** | aspnet ~200MB vs SDK ~1.7GB | Smaller image = smaller attack surface, faster deployment |")

    Set-Content -LiteralPath $reportPath -Value $lines -Encoding UTF8
    return $reportPath
}

# ==============================
# MAIN EXECUTION
# ==============================

Test-DockerAvailable

if (-not (Test-Path -LiteralPath $ConfigPath)) {
    throw "Target config was not found: $ConfigPath"
}

if ($BuildCompose) {
    Write-Host "Building docker compose images ($ComposeFile)..."
    $build = Invoke-External -FilePath "docker" -Arguments @("compose", "-f", $ComposeFile, "build")
    Write-Host $build.Output
    if ($build.ExitCode -ne 0) {
        throw "docker compose build failed."
    }
}

if ($StartCompose) {
    Write-Host "Starting docker compose stack ($ComposeFile)..."
    $up = Invoke-External -FilePath "docker" -Arguments @("compose", "-f", $ComposeFile, "up", "-d")
    Write-Host $up.Output
    if ($up.ExitCode -ne 0) {
        throw "docker compose up -d failed."
    }
}

$config = Get-Content -Raw -LiteralPath $ConfigPath | ConvertFrom-Json
$targets = @($config.targets)
$effectiveProfile = if ($Profile -eq "compose") { "multi" } else { $Profile }
if ($effectiveProfile -ne "all") {
    $targets = @($targets | Where-Object { $_.profile -eq $effectiveProfile })
}

# ========================================================================
# ============================ GỌI HÀM TẤN CÔNG ===========================
# Chạy TẤN CÔNG 1 (quét image) và TẤN CÔNG 2 (quét runtime)
# cho từng target (Web, Gateway, Identity, Product, Order)
# ========================================================================
foreach ($target in $targets) {
    Test-StaticImage -Target $target
    Test-ContainerRuntime -Target $target -Profile $effectiveProfile
}

# GỌI HÀM TẤN CÔNG 3: Quét file docker-compose
Test-ComposeSecurity -ProfileName $Profile

$Results |
    Sort-Object Target, Case |
    Format-Table Target, Case, Status, Severity -AutoSize

$failCount = @($Results | Where-Object { $_.Status -eq "FAIL" }).Count
$warnCount = @($Results | Where-Object { $_.Status -eq "WARN" }).Count
$passCount = @($Results | Where-Object { $_.Status -eq "PASS" }).Count

Write-Host ""
Write-Host "Summary: PASS=$passCount WARN=$warnCount FAIL=$failCount"

if (-not $NoReport) {
    $report = Write-ComparisonReport
    Write-Host "Comparison report: $report"
}

if ($FailOnFinding -and $failCount -gt 0) {
    exit 1
}