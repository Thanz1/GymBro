# GIẢI THÍCH CÁC LOẠI TẤN CÔNG VÀ CÁCH DOCKER PHÒNG VỆ TRONG GYMBRO

File này giải thích chi tiết từng kịch bản kiểm thử trong `run-docker-attack-tests.ps1`:
- **Tấn công dựa trên nguyên lý gì?**
- **Rủi ro là gì nếu không phòng vệ?**
- **Docker/multi-stage phòng vệ ra sao?**
- **Hiện trạng GymBro (single-stage vs multi-stage hardened)?**

Các kịch bản là **mô phỏng an toàn**: không khai thác phá hoại, không tấn công hệ thống bên ngoài, không thay đổi dữ liệu nghiệp vụ.

---

## 1. Mô hình đe dọa được giả định

Các test giả định một số tình huống thường gặp trong bảo mật container:

| Mã | Tình huống | Phạm vi ảnh hưởng |
|:--:|------------|-------------------|
| TH-1 | Attacker lấy được image từ registry hoặc máy build | Có thể chạy container tạm từ image, đọc nội dung image |
| TH-2 | Attacker có quyền chạy container tạm từ image | Có thể chạy lệnh shell để đọc file, kiểm tra công cụ |
| TH-3 | Attacker có được shell trong container do lỗ hổng ứng dụng (RCE) | Có thể thực thi lệnh trong container đang chạy thật |
| TH-4 | Attacker đọc được file Compose hoặc cấu hình triển khai | Có thể đọc password, port, cấu hình từ file yaml |
| TH-5 | Attacker quét được các cổng đang publish ra host | Có thể kết nối trực tiếp đến service nội bộ qua port public |

Mục tiêu phòng vệ là **giảm thiểu thiệt hại nếu một trong các tình huống trên xảy ra**.

---

## 2. Tóm tắt các nhóm tấn công

| Nhóm | Mã test | Tấn công / Rủi ro chính | Phòng vệ chính | Single | Multi |
|------|---------|------------------------|----------------|:-----:|:-----:|
| **Image content** | IMG-02, IMG-03, IMG-05 | Image chứa SDK (~1.7GB), source code, build tools | Multi-stage: tách SDK build, COPY --from=build | ❌ FAIL | ✅ PASS |
| **Image hardening** | IMG-01, IMG-04, IMG-06, IMG-09 | User root, debug files, ghi file trái phép, thiếu HEALTHCHECK | `USER gymbro`, `DebugSymbols=false`, `chown`, `HEALTHCHECK` | ❌ FAIL | ✅ PASS |
| **Runtime privilege** | RUN-01, RUN-03, RUN-04 | Container chạy root, còn capabilities, leo thang setuid | `USER gymbro`, `cap_drop: ALL`, `no-new-privileges` | ❌ FAIL | ✅ PASS |
| **Secret leakage** | IMG-07, IMG-08, CMP-01 | Password trong image history/env hoặc Compose | `.env` (Multi), hard-code (Single baseline) | ℹ️ INFO | ℹ️ INFO |
| **Network exposure** | RUN-02, CMP-03 | Filesystem ghi được, port nội bộ public | Giữ nguyên để demo/test | ⚠️ WARN | ⚠️ WARN |
| **Resource limits** | RUN-05, CMP-04 | Không memory limits, thiếu hardening compose | `deploy.resources.limits`, `cap_drop`, `no-new-privileges` | ❌ FAIL | ✅ PASS |

---

## 3. NHÓM 1: Image content — Rò rỉ SDK, source code, build tools

### 3.1. IMG-01 và RUN-01: Container chạy quyền root

#### Nguyên lý tấn công

Nếu container chạy bằng user `root` (UID=0), khi ứng dụng bị khai thác RCE (Remote Code Execution) hoặc attacker có shell trong container, attacker có quyền cao nhất bên trong container. Root trong container không đồng nghĩa root trên host, nhưng vẫn nguy hiểm vì attacker có thể:
- Ghi/sửa file ứng dụng, cài backdoor.
- Đọc toàn bộ file hệ thống (cấu hình, secret, database file).
- Lợi dụng volume mount sai quyền để ghi vào host.
- Kết hợp với lỗ hổng kernel/container runtime để container escape.

#### Cách test

```powershell
# Test trên image (IMG-01)
docker run --rm --entrypoint /bin/sh gymbro-web:single -c "id -u; id -un"
docker run --rm --entrypoint /bin/sh gymbro-web:multi -c "id -u; id -un"

# Test trên container thật (RUN-01)
docker inspect gymbro_web_single --format "{{.Config.User}}"
docker inspect gymbro_web_multi --format "{{.Config.User}}"
```

#### Docker phòng vệ

Dockerfile nên tạo user thường và chạy ứng dụng bằng `USER` ở final stage:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN groupadd -r gymbro && useradd -r -g gymbro -d /home/gymbro -m gymbro
COPY --from=build /app/publish .
RUN chown -R gymbro:gymbro /app /home/gymbro

USER gymbro
ENTRYPOINT ["dotnet", "GymBro.Web.dll"]
```

#### Hiện trạng GymBro

| Profile | Dockerfile.single | Dockerfile.multistage |
|---------|-------------------|-----------------------|
| **Single** | ❌ **FAIL** — `FROM aspnet:8.0`, không khai báo `USER`, mặc định root | ❌ **FAIL** — tương tự, không có USER |
| **Multi** | — | ✅ **PASS** — có `USER gymbro` + `chown -R gymbro:gymbro` |

#### Kết quả thực tế khi chạy test
- `gymbro-web:single` → UID=0 (root) → **FAIL**
- `gymbro-web:multi` → UID=1000 (gymbro) → **PASS**

---

### 3.2. IMG-02: Runtime image còn .NET SDK

#### Nguyên lý tấn công

SDK là công cụ build, không cần thiết khi chạy ứng dụng đã publish. Nếu runtime image còn SDK, attacker có thêm công cụ để:
- Biên dịch/chạy mã độc .NET ngay trong container.
- Dùng CLI để dò môi trường runtime (dotnet-ef, dotnet tool...).
- Làm tăng diện tích lỗ hổng vì SDK có nhiều thành phần hơn runtime.

**Kích thước image minh họa:**
- `mcr.microsoft.com/dotnet/sdk:8.0` ≈ **1.7 GB** (chứa SDK, build tools, template...)
- `mcr.microsoft.com/dotnet/aspnet:8.0` ≈ **200 MB** (chỉ chạy .NET)

#### Cách test

```powershell
# Test xem có SDK không
docker run --rm --entrypoint /bin/sh gymbro-web:single -c "dotnet --list-sdks"
docker run --rm --entrypoint /bin/sh gymbro-web:multi -c "dotnet --list-sdks"

# So sánh kích thước
docker image inspect gymbro-web:single --format "{{.Size}}"
docker image inspect gymbro-web:multi --format "{{.Size}}"
```

#### Docker phòng vệ

Multi-stage build tách SDK ra stage build và chỉ dùng ASP.NET runtime ở final stage:

```dockerfile
# STAGE 1: Build — dùng SDK
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
RUN dotnet publish ... -o /app/publish

# STAGE 2: Runtime — dùng aspnet thuần
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
COPY --from=build /app/publish .
```

#### Hiện trạng GymBro

| Profile | Kết quả | Giải thích |
|---------|:-------:|------------|
| **Single** | ❌ **FAIL** | `FROM dotnet/sdk:8.0` → còn SDK |
| **Multi** | ✅ **PASS** | `FROM dotnet/aspnet:8.0` → hết SDK |

#### Kết quả thực tế khi chạy test
- `gymbro-web:single` ~ **1.7 GB** (còn SDK) → **FAIL**
- `gymbro-web:multi` ~ **360 MB** (chỉ runtime) → **PASS**
- Multi nhỏ hơn **~5 lần**, giảm đáng kể bề mặt tấn công.

---

### 3.3. IMG-03: Rò rỉ source code / build file

#### Nguyên lý tấn công

Nếu image chứa `.cs`, `.csproj`, `.sln`, attacker có thể:
- Đọc **toàn bộ source code** của ứng dụng.
- Tìm điểm yếu trong logic, hard-coded credential, cấu trúc database.
- Hiểu kiến trúc microservice để lên kế hoạch tấn công mở rộng.

#### Cách test

```powershell
docker run --rm --entrypoint /bin/sh gymbro-web:single -c "find /app /src -name '*.cs' -o -name '*.csproj' -o -name '*.sln' 2>/dev/null | head -20"
docker run --rm --entrypoint /bin/sh gymbro-web:multi -c "find /app /src -name '*.cs' -o -name '*.csproj' -o -name '*.sln' 2>/dev/null | head -20"
```

#### Docker phòng vệ

Multi-stage chỉ copy **published artifact** (đã compile) sang final stage:

```dockerfile
# ← BUILD STAGE: copy TOÀN BỘ source, build xong
FROM dotnet/sdk:8.0 AS build
COPY . .                     # ← Ở đây vẫn có source
RUN dotnet publish ... -o /app/publish

# ← RUNTIME STAGE: chỉ copy DLL đã publish
FROM dotnet/aspnet:8.0
COPY --from=build /app/publish .  # ← Không copy source
```

#### Hiện trạng GymBro

| Profile | Kết quả |
|---------|:-------:|
| **Single** | ❌ **FAIL** — copy cả source, `/src` còn đầy đủ .cs, .csproj |
| **Multi** | ✅ **PASS** — final stage không có `.cs` hay `.csproj` |

---

### 3.4. IMG-04: File debug / development

#### Nguyên lý tấn công

File `.pdb` (Program Debug Database) chứa thông tin debug có thể giúp attacker:
- Xác định dòng code chính xác khi xảy ra lỗi.
- `appsettings.Development.json` có thể chứa secret thật (password SMTP, API key...).

#### Cách test

```powershell
docker run --rm --entrypoint /bin/sh gymbro-web:single -c "find /app -name '*.pdb' -o -name 'appsettings.Development.json' 2>/dev/null | head -20"
```

#### Docker phòng vệ

- Dùng `-c Release` và thêm `/p:DebugSymbols=false` trong `dotnet publish`.
- Không copy `appsettings.Development.json` vào final stage.
- Có thể dùng `.dockerignore` để loại trừ.

#### Hiện trạng GymBro

| Profile | Kết quả | Ghi chú |
|---------|:-------:|---------|
| **Single** | ⚠️ **WARN** | Còn file `.pdb` |
| **Multi** | ✅ **PASS** | Đã cấu hình `/p:DebugSymbols=false /p:DebugType=none` từ 6/2026 |

Multi-stage đã loại bỏ `.pdb` hoàn toàn nhờ `DebugSymbols=false`. `appsettings.Development.json` vẫn còn trong image (đọc qua `appsettings.json` chung).

---

### 3.5. IMG-05: Build tools trong runtime

#### Nguyên lý tấn công

Build tools (`gcc`, `make`, `git`, `dotnet-ef`) là công cụ dành cho phát triển, không cần thiết lúc runtime. Nếu còn:
- `gcc` + `make` → biên dịch mã C/C++ độc hại.
- `git` → clone repository chứa mã độc.
- `dotnet-ef` → can thiệp database.

#### Cách test

```powershell
docker run --rm --entrypoint /bin/sh gymbro-web:single -c "command -v dotnet-ef gcc make git 2>/dev/null"
```

#### Docker phòng vệ

`dotnet/sdk:8.0` chứa đầy đủ build tools (gcc, make, git...). `dotnet/aspnet:8.0` hoàn toàn không có.

#### Hiện trạng GymBro

| Profile | Kết quả |
|---------|:-------:|
| **Single** | ❌ **FAIL** — còn `gcc`, `make`, `git`, `dotnet-ef` |
| **Multi** | ✅ **PASS** — không có build tools |

---

## 4. NHÓM 2: Runtime privilege — Leo thang quyền trong container

### 4.1. RUN-02: Root filesystem read-only

#### Nguyên lý tấn công

Nếu container có filesystem ghi được (read-write), attacker sau khi chiếm được shell có thể:
- Ghi đè file ứng dụng bằng mã độc.
- Tạo file mới trong thư mục ứng dụng.
- Cài đặt công cụ leo thang quyền.

#### Cách test

```powershell
# Test trên image
docker run --rm --entrypoint /bin/sh gymbro-web:multi -c "(touch /app/attack_test 2>/dev/null && echo WRITABLE) || echo BLOCKED"

# Test trên container
docker inspect gymbro_web_multi --format "{{.HostConfig.ReadonlyRootfs}}"
```

#### Docker phòng vệ

- Compose: `read_only: true` cho service.
- Dockerfile: non-root user + `chown` hạn chế quyền ghi.

#### Hiện trạng GymBro

| Profile | Kết quả | Ghi chú |
|---------|:-------:|---------|
| **Single** | ❌ **FAIL** — chạy root, có thể ghi file | ⚠️ **WARN** — không có `read_only` |
| **Multi** | ✅ **PASS** — non-root user (không ghi được /app) | ⚠️ **WARN** — không có `read_only` |

Lưu ý: `read_only: true` được test trước đó nhưng gây lỗi migration (EF Core cần ghi file). Do đó hiện tại giữ **WARN** thay vì bật `read_only`.

---

### 4.2. RUN-03: Linux capabilities

#### Nguyên lý tấn công

Linux capabilities là các quyền kernel đặc biệt (CAP_NET_ADMIN, CAP_SYS_ADMIN...). Mặc định Docker container có khoảng 14 capabilities. Nếu không drop, attacker có thể:
- `CAP_NET_ADMIN`: thay đổi cấu hình mạng container.
- `CAP_SYS_ADMIN`: mount filesystem, thay đổi hostname...
- `CAP_DAC_OVERRIDE`: bypass quyền đọc/ghi file.

#### Cách test

```powershell
docker inspect gymbro_web_single --format "{{json .HostConfig.CapDrop}}"
docker inspect gymbro_web_multi --format "{{json .HostConfig.CapDrop}}"
```

#### Docker phòng vệ

```yaml
# docker-compose.yml
services:
  app:
    cap_drop:
      - ALL
    cap_add:
      - NET_BIND_SERVICE  # Chỉ cần quyền bind port
```

#### Hiện trạng GymBro

| Profile | cap_drop | Kết quả |
|---------|----------|:-------:|
| **Single** | ❌ Không có | ⚠️ **WARN** |
| **Multi** | ✅ `cap_drop: ALL` + `cap_add: NET_BIND_SERVICE` | ✅ **PASS** |

---

### 4.3. RUN-04: no-new-privileges

#### Nguyên lý tấn công

`no-new-privileges` ngăn process con nhận quyền cao hơn process cha thông qua setuid binaries (như `sudo`, `su`, `passwd`). Nếu không có:
- Attacker có thể chạy `sudo` để leo lên root (nếu user trong sudoers).
- Attacker có thể khai thác lỗ hổng setuid binary để leo thang quyền.

#### Cách test

```powershell
docker inspect gymbro_web_single --format "{{json .HostConfig.SecurityOpt}}"
docker inspect gymbro_web_multi --format "{{json .HostConfig.SecurityOpt}}"
```

#### Docker phòng vệ

```yaml
services:
  app:
    security_opt:
      - no-new-privileges:true
```

#### Hiện trạng GymBro

| Profile | no-new-privileges | Kết quả |
|---------|:-----------------:|:-------:|
| **Single** | ❌ Không có | ⚠️ **WARN** |
| **Multi** | ✅ Có | ✅ **PASS** |

---

## 5. NHÓM 3: Secret leakage — Rò rỉ mật khẩu và khóa bí mật

### 5.1. IMG-07: Secret trong image history

#### Nguyên lý tấn công

Docker image lưu lịch sử các layer. Nếu một layer chứa lệnh `RUN`, `ENV`, hoặc `ARG` có password, password đó sẽ vĩnh viễn tồn tại trong history dù đã xóa ở layer sau.

```dockerfile
# Password sẽ còn trong history dù dòng dưới đã xóa
RUN echo "Password=GymBro@2024" > /app/config.txt
RUN rm /app/config.txt
```

#### Cách test

```powershell
docker history --no-trunc gymbro-web:single | findstr "Password"
```

#### Hiện trạng GymBro

| Profile | Kết quả | Ghi chú |
|---------|:-------:|---------|
| **Single** | ✅ **PASS** | Không có secret trong Dockerfile |
| **Multi** | ✅ **PASS** | Không có secret trong Dockerfile |

Cả 2 đều an toàn vì không bake secret vào Dockerfile.

---

### 5.2. IMG-08: Secret trong image ENV

#### Nguyên lý tấn công

Biến môi trường (ENV) trong Dockerfile có thể đọc được bằng `docker image inspect`. Nếu set secret trong ENV, ai có image cũng có thể đọc được.

```dockerfile
ENV MSSQL_SA_PASSWORD=GymBro@2024  # ❌ Có thể inspect được
```

#### Cách test

```powershell
docker image inspect gymbro-web:single --format "{{json .Config.Env}}"
```

#### Hiện trạng GymBro

| Profile | Kết quả | Ghi chú |
|---------|:-------:|---------|
| **Single** | ✅ **PASS** | Không có ENV secret trong image |
| **Multi** | ✅ **PASS** | Không có ENV secret trong image |

Cả 2 chỉ set `ASPNETCORE_URLS=http://+:8080` trong Dockerfile (không phải secret). Password được truyền qua `-e` trong docker-compose (runtime, không lưu trong image).

---

### 5.3. CMP-01: Secret hard-code trong docker-compose

#### Nguyên lý tấn công

Docker-compose.yml có thể chứa password trực tiếp. Nếu file này bị leak (qua git, CI/CD log, config map), attacker có toàn bộ thông tin đăng nhập.

#### Cách test

```powershell
Select-String -Path docker-compose-single.yml -Pattern "Password=|MSSQL_SA_PASSWORD|Jwt__Key" -AllMatches
```

#### Docker phòng vệ

- Dùng file `.env` riêng, không commit vào git.
- Dùng Docker secrets (đọc file ở runtime).
- Dùng secret manager (Azure Key Vault, AWS Secrets Manager...).

#### Hiện trạng GymBro

| Profile | Kết quả | Ghi chú |
|---------|:-------:|---------|
| **Single** | ℹ️ **INFO** | Password SQL, RabbitMQ, SMTP hard-code |
| **Multi** | ℹ️ **INFO** | Password SQL, RabbitMQ hard-code |

Cả 2 compose đều có `MSSQL_SA_PASSWORD` và `RABBITMQ_DEFAULT_PASS` trực tiếp trong file. **Đây là lựa chọn có chủ đích** cho dự án nghiên cứu — giúp bạn bè/giảng viên clone về chạy được ngay mà không cần cấu hình thêm. Trong production, khuyến nghị dùng `.env`, Docker secrets hoặc secret manager.

---

## 6. NHÓM 4: Network exposure — Phơi bày cổng nội bộ

### 6.1. RUN-06 và CMP-03: Cổng nội bộ public

#### Nguyên lý tấn công

Port `1433` (SQL Server), `5672` (RabbitMQ), `15672` (RabbitMQ Web), `7001-7003` (API) đều là service nội bộ. Nếu publish ra host:
- Attacker có thể kết nối trực tiếp đến SQL Server từ máy host (dùng `localhost:1433`).
- Tấn công brute-force vào RabbitMQ management.
- Gọi API trực tiếp (bỏ qua gateway, bỏ qua authentication).

#### Cách test

```powershell
docker inspect gymbro_web_multi --format "{{json .NetworkSettings.Ports}}"
```

#### Docker phòng vệ

Trong production, chỉ publish Web và Gateway:
```yaml
services:
  db:
    ports:
      # Không publish ra host, chỉ trong network nội bộ
  product_api:
    ports:
      - "7002:8080"  # ❌ Không cần public
  gymbro_web:
    ports:
      - "5000:8080"  # ✅ Chỉ Web cần public
```

#### Hiện trạng GymBro

| Profile | Port public | Kết quả |
|---------|------------|:-------:|
| **Single** | `1434, 5673, 15673, 7011-7013, 8001, 5001` | ⚠️ **WARN** |
| **Multi** | `1433, 5672, 15672, 7001-7003, 8000, 5000` | ⚠️ **WARN** |

Cả 2 đều publish toàn bộ port cho mục đích development/demo.

---

## 7. NHÓM 5: Security hardening — Các biện pháp tăng cường

### 7.1. IMG-09: HEALTHCHECK

#### Nguyên lý

Thiếu HEALTHCHECK → Docker không biết container có ứng dụng đang sống hay đã chết (nhưng process vẫn chạy). Docker sẽ không tự động restart.

#### Cách test

```powershell
docker image inspect gymbro-web:single --format "{{json .Config.Healthcheck}}"
```

#### Docker phòng vệ

```dockerfile
HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
    CMD curl -f http://localhost:8080/ || exit 1
```

#### Hiện trạng GymBro

| Profile | Kết quả | Ghi chú |
|---------|:-------:|---------|
| **Single** | ⚠️ **WARN** | Không có HEALTHCHECK (cần cài curl) |
| **Multi** | ✅ **PASS** | Có HEALTHCHECK với curl |

---

### 7.2. RUN-05: Memory limits

#### Nguyên lý

Không giới hạn RAM → một container có thể chiếm toàn bộ RAM host (DoS). Một lỗi memory leak trong ứng dụng có thể làm sập toàn bộ hệ thống.

#### Cách test

```powershell
docker inspect gymbro_web_multi --format "{{.HostConfig.Memory}}"
```

#### Docker phòng vệ

```yaml
services:
  app:
    deploy:
      resources:
        limits:
          memory: 512M
```

#### Hiện trạng GymBro

| Profile | Kết quả | Ghi chú |
|---------|:-------:|---------|
| **Single** | ⚠️ **WARN** | Không có memory limits (trừ SQL Server 1024M) |
| **Multi** | ✅ **PASS** | Có limits: API 512M, Gateway 256M, DB 2048M |

---

### 7.3. CMP-04: Security hardening trong compose

#### Nguyên lý

Docker Compose có thể cấu hình các biện pháp hardening runtime: `cap_drop`, `no-new-privileges`, `memory limits`. Nếu thiếu, container dễ bị tấn công hơn khi có lỗ hổng.

#### Cách test

```powershell
$raw = Get-Content docker-compose.yml -Raw
$raw -match "cap_drop:"    # Kiểm tra có cap_drop không
$raw -match "no-new-privileges:"  # Kiểm tra no-new-privileges
$raw -match "deploy:"  # Kiểm tra có memory limits không
```

#### Hiện trạng GymBro

| Profile | cap_drop | no-new-priv | memory limits | Kết quả |
|---------|:--------:|:-----------:|:-------------:|:-------:|
| **Single** | ❌ | ❌ | ❌ (trừ DB) | ❌ **FAIL** |
| **Multi** | ✅ | ✅ | ✅ | ✅ **PASS** |

---

## 8. Bảng tổng hợp tất cả phương thức tấn công

| # | Mã test | Phương thức tấn công | Cách test | Single | Multi | Bảo vệ nhờ |
|:-:|:-------:|----------------------|-----------|:------:|:-----:|------------|
| 1 | IMG-00 | Đo kích thước image, image lớn hơn = bề mặt tấn công lớn hơn | `docker image inspect --format "{{.Size}}"` | ~1.7GB | ~350MB | Multi-stage |
| 2 | **IMG-01** | **Chạy root → attacker có toàn quyền trong container** | `docker run --entrypoint /bin/sh â¦ -c "id -u"` | ❌ FAIL | ✅ PASS | `USER gymbro` |
| 3 | **IMG-02** | **Còn .NET SDK → attacker build mã độc** | `docker run â¦ "dotnet --list-sdks"` | ❌ FAIL | ✅ PASS | Multi-stage tách build/runtime |
| 4 | **IMG-03** | **Leak source code → attacker đọc logic, secret** | `find /app /src -name "*.cs"` | ❌ FAIL | ✅ PASS | `COPY --from=build` |
| 5 | IMG-04 | File debug/development lộ thông tin | `find /app -name "*.pdb"` | ⚠️ WARN | ✅ PASS | `DebugSymbols=false` (Multi) |
| 6 | **IMG-05** | **Còn build tools → attacker biên dịch mã độc** | `command -v gcc make git` | ❌ FAIL | ✅ PASS | Multi-stage |
| 7 | IMG-06 | Ghi file trái phép → cài backdoor | `touch /app/attack_test` | ❌ FAIL | ✅ PASS | Non-root + chown |
| 8 | IMG-07 | Secret trong image history | `docker history --no-trunc` | ✅ PASS | ✅ PASS | Không bake secret |
| 9 | IMG-08 | Secret trong image env | `docker image inspect .Config.Env` | ✅ PASS | ✅ PASS | Không bake secret |
| 10 | IMG-09 | Thiếu HEALTHCHECK → container chết không được phát hiện | `docker image inspect .Config.Healthcheck` | ⚠️ WARN | ✅ PASS | `HEALTHCHECK` |
| 11 | **RUN-01** | **Container chạy root → attacker leo thang quyền** | `docker inspect .Config.User` | ❌ FAIL | ✅ PASS | `USER gymbro` |
| 12 | RUN-02 | Filesystem ghi được → attacker cài backdoor | `docker inspect .HostConfig.ReadonlyRootfs` | ⚠️ WARN | ⚠️ WARN | `read_only: true` (future) |
| 13 | **RUN-03** | **Còn capabilities → attacker leo thang kernel** | `docker inspect .HostConfig.CapDrop` | ⚠️ WARN | ✅ PASS | `cap_drop: ALL` |
| 14 | **RUN-04** | **Leo thang setuid → attacker lên root** | `docker inspect .HostConfig.SecurityOpt` | ⚠️ WARN | ✅ PASS | `no-new-privileges` |
| 15 | RUN-05 | Không memory limits → DoS | `docker inspect .HostConfig.Memory` | ⚠️ WARN | ✅ PASS | Memory limits |
| 16 | RUN-06 | Port nội bộ public → attacker kết nối trực tiếp | `docker inspect .NetworkSettings.Ports` | ℹ️ INFO | ℹ️ INFO | Cần thu nhỏ port |
| 17 | **CMP-01** | **Secret hard-code trong compose** | `Select-String "Password=" docker-compose.yml` | ℹ️ INFO | ✅ PASS | `.env` (Multi), hard-code (Single) |
| 18 | CMP-02 | Dev mode bật debug, leak exception | `Select-String "Development" docker-compose.yml` | ⚠️ WARN | ⚠️ WARN | Cần Production mode |
| 19 | CMP-03 | Over-publish port | Đếm port public trong compose | ⚠️ WARN | ⚠️ WARN | Cần thu nhỏ |
| 20 | CMP-04 | Thiếu security hardening | Kiểm tra cap_drop, no-new-priv, memory | ❌ FAIL | ✅ PASS | Multi-stage |

---

## 9. Đề xuất cải thiện

### Các vấn đề còn tồn tại ở cả Single và Multi

| Mã | Vấn đề | Mức ưu tiên | Đề xuất |
|:--:|--------|:-----------:|---------|
| IMG-04 | Còn `.pdb` và `appsettings.Development.json` (Single) | Medium | Đã fix cho Multi với `/p:DebugSymbols=false`, Single cần làm tương tự |
| RUN-02 | `read_only: true` tạm tắt (gây lỗi migration) | Thấp | Cần chỉnh lại migration để hỗ trợ read-only |
| CMP-01 | Password hard-code trong compose (Single) | Thấp | Đã fix cho Multi với `.env`, Single giữ nguyên (baseline) |
| CMP-02 | `ASPNETCORE_ENVIRONMENT=Development` | Medium | Dùng Production cho security test |
| CMP-03 | Quá nhiều port public | Thấp | Demo nên giữ, production chỉ expose Web/Gateway |

### Kết quả tổng kết

| Chỉ số | Single-stage | Multi-stage hardened | Cải thiện |
|--------|:-----------:|:-------------------:|:---------:|
| **PASS** | 3/20 | **13/20** | ✅ +10 |
| **WARN** | 10/20 | 6/20 | ✅ -4 |
| **FAIL** | **6/20** | 0/20 | ✅ -6 |
| **Kích thước** | ~1.7GB | ~350MB | Nhỏ hơn ~5 lần |
| **Runtime hardening** | 0/5 | 4/5 | cap_drop, no-new-priv, user, memory |
| **Chi phí build** | ~3 phút | ~3 phút (cached ~1 phút) | Tương đương (nhờ layer cache) |