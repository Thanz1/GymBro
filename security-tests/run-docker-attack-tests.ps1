[CmdletBinding()]
param(
    [ValidateSet("compose", "research", "all")]
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
    $ComposeFile = Join-Path $PSScriptRoot "..\docker-compose.yml"
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

function Test-StaticImage {
    param($Target)

    $name = [string]$Target.name
    $image = [string]$Target.image

    if (-not (Test-ImageExists -Image $image)) {
        Add-TestResult -Target $name -Case "IMG-00 image exists" -Status "SKIP" -Severity "Info" -Evidence "Image not found: $image" -Recommendation "Build the image first with docker compose build or docker build."
        return
    }

    Add-TestResult -Target $name -Case "IMG-00 image size" -Status "INFO" -Severity "Info" -Evidence "$image = $(Get-ImageSizeText -Image $image)" -Recommendation "Use this value to compare single-stage and secure multi-stage images."

    $userResult = Invoke-ImageShell -Image $image -Command 'id -u; id -un 2>/dev/null'
    if ($userResult.ExitCode -ne 0) {
        Add-TestResult -Target $name -Case "IMG-01 runtime user" -Status "WARN" -Severity "Medium" -Evidence $userResult.Output -Recommendation "Could not read runtime user. Check image shell support."
    }
    else {
        $lines = @($userResult.Output -split "`n" | ForEach-Object { $_.Trim() } | Where-Object { $_ })
        $uid = if ($lines.Count -gt 0) { $lines[0] } else { "" }
        $uname = if ($lines.Count -gt 1) { $lines[1] } else { "" }
        if ($uid -eq "0" -or [string]::IsNullOrWhiteSpace($uid)) {
            Add-TestResult -Target $name -Case "IMG-01 runtime user is non-root" -Status "FAIL" -Severity "High" -Evidence "uid=$uid user=$uname" -Recommendation "Create a non-root user in final stage and set USER."
        }
        else {
            Add-TestResult -Target $name -Case "IMG-01 runtime user is non-root" -Status "PASS" -Severity "High" -Evidence "uid=$uid user=$uname" -Recommendation "Least privilege is enabled at image level."
        }
    }

    $sdkResult = Invoke-ImageShell -Image $image -Command 'dotnet --list-sdks 2>/dev/null'
    if ([string]::IsNullOrWhiteSpace($sdkResult.Output)) {
        Add-TestResult -Target $name -Case "IMG-02 no .NET SDK in runtime" -Status "PASS" -Severity "High" -Evidence "dotnet --list-sdks returned no SDK" -Recommendation "This proves the final image does not carry the SDK."
    }
    else {
        Add-TestResult -Target $name -Case "IMG-02 no .NET SDK in runtime" -Status "FAIL" -Severity "High" -Evidence $sdkResult.Output -Recommendation "Final image should use mcr.microsoft.com/dotnet/aspnet, not dotnet/sdk."
    }

    $sourceCommand = "find /app /src -type f \( -name '*.cs' -o -name '*.csproj' -o -name '*.sln' \) 2>/dev/null | head -n 20"
    $sourceResult = Invoke-ImageShell -Image $image -Command $sourceCommand
    if ([string]::IsNullOrWhiteSpace($sourceResult.Output)) {
        Add-TestResult -Target $name -Case "IMG-03 no source/build files leaked" -Status "PASS" -Severity "High" -Evidence "No .cs/.csproj/.sln files found under /app or /src" -Recommendation "Only published artifacts are copied to final image."
    }
    else {
        Add-TestResult -Target $name -Case "IMG-03 no source/build files leaked" -Status "FAIL" -Severity "High" -Evidence $sourceResult.Output -Recommendation "Do not copy the whole source tree into the final image."
    }

    $debugCommand = "find /app -type f \( -name '*.pdb' -o -name 'appsettings.Development.json' \) 2>/dev/null | head -n 20"
    $debugResult = Invoke-ImageShell -Image $image -Command $debugCommand
    if ([string]::IsNullOrWhiteSpace($debugResult.Output)) {
        Add-TestResult -Target $name -Case "IMG-04 no debug/development files" -Status "PASS" -Severity "Medium" -Evidence "No .pdb or appsettings.Development.json found under /app" -Recommendation "Keep DebugSymbols=false and remove development config from final image."
    }
    else {
        Add-TestResult -Target $name -Case "IMG-04 no debug/development files" -Status "WARN" -Severity "Medium" -Evidence $debugResult.Output -Recommendation "Consider removing .pdb and appsettings.Development.json from runtime image."
    }

    $toolCommand = 'command -v dotnet-ef 2>/dev/null; command -v gcc 2>/dev/null; command -v make 2>/dev/null; command -v git 2>/dev/null'
    $toolResult = Invoke-ImageShell -Image $image -Command $toolCommand
    if ([string]::IsNullOrWhiteSpace($toolResult.Output)) {
        Add-TestResult -Target $name -Case "IMG-05 no common build tools" -Status "PASS" -Severity "Medium" -Evidence "dotnet-ef/gcc/make/git were not found" -Recommendation "Runtime image stays minimal."
    }
    else {
        Add-TestResult -Target $name -Case "IMG-05 no common build tools" -Status "FAIL" -Severity "Medium" -Evidence $toolResult.Output -Recommendation "Remove build tools from final image with multi-stage build."
    }

    $writeCommand = '(touch /app/.attack_write_test 2>/dev/null && rm -f /app/.attack_write_test && echo WRITABLE) || echo BLOCKED'
    $writeResult = Invoke-ImageShell -Image $image -Command $writeCommand
    if ($writeResult.Output -match "BLOCKED") {
        Add-TestResult -Target $name -Case "IMG-06 simulated write to /app" -Status "PASS" -Severity "Medium" -Evidence "Write to /app was blocked" -Recommendation "If the app must write files, mount a dedicated volume."
    }
    else {
        Add-TestResult -Target $name -Case "IMG-06 simulated write to /app" -Status "WARN" -Severity "Medium" -Evidence $writeResult.Output -Recommendation "Use read_only: true in Compose and mount only writable folders."
    }

    $history = Invoke-External -FilePath "docker" -Arguments @("history", "--no-trunc", $image)
    if ($history.ExitCode -eq 0 -and $history.Output -match $SecretPattern) {
        Add-TestResult -Target $name -Case "IMG-07 no secrets in image history" -Status "FAIL" -Severity "High" -Evidence (Redact-Secrets $history.Output) -Recommendation "Do not put passwords/keys in Dockerfile RUN/ENV/ARG."
    }
    else {
        Add-TestResult -Target $name -Case "IMG-07 no secrets in image history" -Status "PASS" -Severity "High" -Evidence "No secret pattern found in docker history" -Recommendation "Keep secrets out of image layers."
    }

    $envInspect = Invoke-External -FilePath "docker" -Arguments @("image", "inspect", $image, "--format", "{{json .Config.Env}}")
    if ($envInspect.ExitCode -eq 0 -and $envInspect.Output -match $SecretPattern) {
        Add-TestResult -Target $name -Case "IMG-08 no secrets in image env" -Status "FAIL" -Severity "High" -Evidence (Redact-Secrets $envInspect.Output) -Recommendation "Do not set secrets with ENV in Dockerfile."
    }
    else {
        Add-TestResult -Target $name -Case "IMG-08 no secrets in image env" -Status "PASS" -Severity "High" -Evidence "No secret pattern found in image env" -Recommendation "Pass secrets at runtime through env/secrets manager."
    }

    if ($RunScanner) {
        $trivy = Get-Command trivy -ErrorAction SilentlyContinue
        if ($trivy) {
            $scan = Invoke-External -FilePath "trivy" -Arguments @("image", "--severity", "CRITICAL,HIGH", "--no-progress", $image)
            $status = if ($scan.Output -match "CRITICAL|HIGH") { "WARN" } else { "PASS" }
            Add-TestResult -Target $name -Case "IMG-09 CVE scan with Trivy" -Status $status -Severity "High" -Evidence $scan.Output -Recommendation "Update base images/packages when High/Critical CVEs exist."
        }
        else {
            $scout = Invoke-External -FilePath "docker" -Arguments @("scout", "version")
            if ($scout.ExitCode -eq 0) {
                $scan = Invoke-External -FilePath "docker" -Arguments @("scout", "cves", $image)
                $status = if ($scan.Output -match "(?i)critical|high") { "WARN" } else { "PASS" }
                Add-TestResult -Target $name -Case "IMG-09 CVE scan with Docker Scout" -Status $status -Severity "High" -Evidence $scan.Output -Recommendation "Update base images/packages when High/Critical CVEs exist."
            }
            else {
                Add-TestResult -Target $name -Case "IMG-09 CVE scan" -Status "SKIP" -Severity "Info" -Evidence "Trivy/Docker Scout not found" -Recommendation "Install Trivy or enable Docker Scout for vulnerability scanning."
            }
        }
    }
}

function Test-ContainerRuntime {
    param($Target)

    $name = [string]$Target.name
    $container = [string]$Target.container

    if (-not (Test-ContainerExists -Container $container)) {
        Add-TestResult -Target $name -Case "RUN-00 container exists" -Status "SKIP" -Severity "Info" -Evidence "Container not found: $container" -Recommendation "Run docker compose up -d or use -StartCompose to test runtime hardening."
        return
    }

    $user = Invoke-External -FilePath "docker" -Arguments @("inspect", $container, "--format", "{{.Config.User}}")
    $userValue = $user.Output.Trim()
    if ([string]::IsNullOrWhiteSpace($userValue) -or $userValue -eq "0" -or $userValue -eq "root") {
        Add-TestResult -Target $name -Case "RUN-01 container user is non-root" -Status "FAIL" -Severity "High" -Evidence "Config.User='$userValue'" -Recommendation "Set USER non-root in Dockerfile final stage."
    }
    else {
        Add-TestResult -Target $name -Case "RUN-01 container user is non-root" -Status "PASS" -Severity "High" -Evidence "Config.User='$userValue'" -Recommendation "Runtime least privilege is enabled."
    }

    $readonly = Invoke-External -FilePath "docker" -Arguments @("inspect", $container, "--format", "{{.HostConfig.ReadonlyRootfs}}")
    if ($readonly.Output.Trim() -eq "true") {
        Add-TestResult -Target $name -Case "RUN-02 root filesystem is read-only" -Status "PASS" -Severity "Medium" -Evidence "ReadonlyRootfs=true" -Recommendation "Mount volumes only for writable folders."
    }
    else {
        Add-TestResult -Target $name -Case "RUN-02 root filesystem is read-only" -Status "WARN" -Severity "Medium" -Evidence "ReadonlyRootfs=$($readonly.Output.Trim())" -Recommendation "Consider read_only: true in production Compose."
    }

    $capDrop = Invoke-External -FilePath "docker" -Arguments @("inspect", $container, "--format", "{{json .HostConfig.CapDrop}}")
    if ($capDrop.Output -match "ALL") {
        Add-TestResult -Target $name -Case "RUN-03 Linux capabilities dropped" -Status "PASS" -Severity "Medium" -Evidence $capDrop.Output -Recommendation "cap_drop: ALL reduces unnecessary kernel privileges."
    }
    else {
        Add-TestResult -Target $name -Case "RUN-03 Linux capabilities dropped" -Status "WARN" -Severity "Medium" -Evidence $capDrop.Output -Recommendation "Consider cap_drop: ALL when the service does not need special capabilities."
    }

    $securityOpt = Invoke-External -FilePath "docker" -Arguments @("inspect", $container, "--format", "{{json .HostConfig.SecurityOpt}}")
    if ($securityOpt.Output -match "no-new-privileges") {
        Add-TestResult -Target $name -Case "RUN-04 no-new-privileges enabled" -Status "PASS" -Severity "Medium" -Evidence $securityOpt.Output -Recommendation "Keep security_opt: no-new-privileges:true."
    }
    else {
        Add-TestResult -Target $name -Case "RUN-04 no-new-privileges enabled" -Status "WARN" -Severity "Medium" -Evidence $securityOpt.Output -Recommendation "Consider security_opt: no-new-privileges:true in Compose."
    }

    $ports = Invoke-External -FilePath "docker" -Arguments @("inspect", $container, "--format", "{{json .NetworkSettings.Ports}}")
    Add-TestResult -Target $name -Case "RUN-05 published ports" -Status "INFO" -Severity "Info" -Evidence $ports.Output -Recommendation "Production should expose only Web/Gateway when possible."
}

function Test-ComposeSecurity {
    if (-not (Test-Path -LiteralPath $ComposeFile)) {
        Add-TestResult -Target "docker-compose.yml" -Case "CMP-00 compose file exists" -Status "SKIP" -Severity "Info" -Evidence "File not found: $ComposeFile" -Recommendation "Pass the correct -ComposeFile path."
        return
    }

    $raw = Get-Content -Raw -LiteralPath $ComposeFile

    $secretMatches = Select-String -LiteralPath $ComposeFile -Pattern $SecretPattern -AllMatches
    if ($secretMatches) {
        $evidenceItems = @()
        foreach ($m in $secretMatches) {
            $evidenceItems += "line $($m.LineNumber): $(Redact-Secrets $m.Line.Trim())"
        }
        Add-TestResult -Target "docker-compose.yml" -Case "CMP-01 no hard-coded secrets" -Status "FAIL" -Severity "High" -Evidence ($evidenceItems -join " | ") -Recommendation "Move passwords/JWT/SMTP secrets to .env, Docker secrets, or a secret manager."
    }
    else {
        Add-TestResult -Target "docker-compose.yml" -Case "CMP-01 no hard-coded secrets" -Status "PASS" -Severity "High" -Evidence "No secret pattern found in Compose" -Recommendation "Keep secrets outside source code."
    }

    if ($raw -match "ASPNETCORE_ENVIRONMENT=Development" -or $raw -match "ASPNETCORE_ENVIRONMENT:\s*Development") {
        Add-TestResult -Target "docker-compose.yml" -Case "CMP-02 not Development for secure deploy" -Status "WARN" -Severity "Medium" -Evidence "Compose declares ASPNETCORE_ENVIRONMENT=Development" -Recommendation "Use Production when evaluating deployment security."
    }
    else {
        Add-TestResult -Target "docker-compose.yml" -Case "CMP-02 not Development for secure deploy" -Status "PASS" -Severity "Medium" -Evidence "Development environment was not found" -Recommendation "Keep Production for runtime security tests."
    }

    $publicPortPatterns = @("1433:1433", "5672:5672", "15672:15672", "7001:8080", "7002:8080", "7003:8080")
    $published = @()
    foreach ($pattern in $publicPortPatterns) {
        if ($raw.Contains($pattern)) {
            $published += $pattern
        }
    }

    if ($published.Count -gt 0) {
        Add-TestResult -Target "docker-compose.yml" -Case "CMP-03 internal ports are not over-published" -Status "WARN" -Severity "Medium" -Evidence ($published -join ", ") -Recommendation "Production should expose only Web/Gateway; keep APIs, SQL Server, and RabbitMQ internal when possible."
    }
    else {
        Add-TestResult -Target "docker-compose.yml" -Case "CMP-03 internal ports are not over-published" -Status "PASS" -Severity "Medium" -Evidence "No common internal public ports found" -Recommendation "Keep network exposure minimal."
    }
}

function Write-MarkdownReport {
    $reportDir = Join-Path $PSScriptRoot "reports"
    New-Item -ItemType Directory -Force -Path $reportDir | Out-Null

    $stamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $reportPath = Join-Path $reportDir "docker-attack-report-$stamp.md"

    $summary = $Results | Group-Object Status | Sort-Object Name
    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add("# Docker Multi-stage Attack Test Report")
    $lines.Add("")
    $lines.Add("- Time: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')")
    $lines.Add("- Profile: $Profile")
    $lines.Add("- Compose file: $ComposeFile")
    $lines.Add("")
    $lines.Add("## Summary")
    $lines.Add("")
    $lines.Add("| Status | Count |")
    $lines.Add("| --- | ---: |")
    foreach ($item in $summary) {
        $lines.Add("| $($item.Name) | $($item.Count) |")
    }
    $lines.Add("")
    $lines.Add("## Details")
    $lines.Add("")
    $lines.Add("| Target | Case | Severity | Status | Evidence | Recommendation |")
    $lines.Add("| --- | --- | --- | --- | --- | --- |")
    foreach ($r in ($Results | Sort-Object Target, Case)) {
        $lines.Add("| $(Escape-MarkdownCell $r.Target) | $(Escape-MarkdownCell $r.Case) | $($r.Severity) | $($r.Status) | $(Escape-MarkdownCell $r.Evidence) | $(Escape-MarkdownCell $r.Recommendation) |")
    }

    Set-Content -LiteralPath $reportPath -Value $lines -Encoding UTF8
    return $reportPath
}

Test-DockerAvailable

if (-not (Test-Path -LiteralPath $ConfigPath)) {
    throw "Target config was not found: $ConfigPath"
}

if ($BuildCompose) {
    Write-Host "Building docker compose images..."
    $build = Invoke-External -FilePath "docker" -Arguments @("compose", "-f", $ComposeFile, "build")
    Write-Host $build.Output
    if ($build.ExitCode -ne 0) {
        throw "docker compose build failed."
    }
}

if ($StartCompose) {
    Write-Host "Starting docker compose stack..."
    $up = Invoke-External -FilePath "docker" -Arguments @("compose", "-f", $ComposeFile, "up", "-d")
    Write-Host $up.Output
    if ($up.ExitCode -ne 0) {
        throw "docker compose up -d failed."
    }
}

$config = Get-Content -Raw -LiteralPath $ConfigPath | ConvertFrom-Json
$targets = @($config.targets)
if ($Profile -ne "all") {
    $targets = @($targets | Where-Object { $_.profile -eq $Profile })
}

foreach ($target in $targets) {
    Test-StaticImage -Target $target
    Test-ContainerRuntime -Target $target
}

Test-ComposeSecurity

$Results |
    Sort-Object Target, Case |
    Format-Table Target, Case, Status, Severity -AutoSize

$failCount = @($Results | Where-Object { $_.Status -eq "FAIL" }).Count
$warnCount = @($Results | Where-Object { $_.Status -eq "WARN" }).Count
$passCount = @($Results | Where-Object { $_.Status -eq "PASS" }).Count

Write-Host ""
Write-Host "Summary: PASS=$passCount WARN=$warnCount FAIL=$failCount"

if (-not $NoReport) {
    $report = Write-MarkdownReport
    Write-Host "Report: $report"
}

if ($FailOnFinding -and $failCount -gt 0) {
    exit 1
}
