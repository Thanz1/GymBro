# Security Tests cho Docker GymBro

Thư mục này chứa bộ kiểm tra mô phỏng tấn công an toàn cho Docker image/container của GymBro. Mục tiêu là tạo bằng chứng thực nghiệm cho báo cáo nghiên cứu khoa học: so sánh **Single-stage thuần túy** (chạy trên SDK image, còn SDK, source code, build tools, root user) với **Multi-stage hardened** (tách build/runtime, non-root user, HEALTHCHECK, capability drop, no-new-privileges, memory limits, .env).

## File trong thư mục

| File | Vai trò |
|------|---------|
| `run-docker-attack-tests.ps1` | Script chính — kiểm tra image, container và Docker Compose. 3 hàm tấn công: `Test-StaticImage`, `Test-ContainerRuntime`, `Test-ComposeSecurity`. Tự sinh báo cáo Markdown. |
| `docker-attack-targets.json` | Danh sách image/container của hai mô hình `single` và `multi` (10 target) |
| `GIAI_THICH_TAN_CONG_DOCKER.md` | ⭐ Giải thích chi tiết từng phương thức tấn công (nguyên lý, cách test, Docker phòng vệ, hiện trạng GymBro) — ~620 dòng |
| `GIAI_THICH_PHUONG_THUC_TAN_CONG.md` | Bảng tổng hợp 20 test case — phù hợp cho phụ lục báo cáo |
| `GHI_CHU_CAC_FILE_TEST_BAO_MAT.md` | Ghi chú vai trò từng file trong thư mục này, hướng dẫn nên đọc file nào |
| `reports/` | Thư mục chứa báo cáo Markdown tự sinh sau khi chạy script |

## Cách chạy nhanh

### Bước 1: Build và chạy cả hai mô hình

```powershell
# Build + chạy Single-stage (thuần túy, SDK image ~1.3-2.4GB)
docker compose -f ../docker-compose-single.yml build
docker compose -f ../docker-compose-single.yml up -d

# Build + chạy Multi-stage hardened (aspnet runtime ~340-440MB)
docker compose -f ../docker-compose-multistage.yml build
docker compose -f ../docker-compose-multistage.yml up -d
```

### Bước 2: Chạy script kiểm tra bảo mật

```powershell
cd security-tests

# So sánh cả Single và Multi (khuyên dùng)
powershell -ExecutionPolicy Bypass -File .\run-docker-attack-tests.ps1 -Profile all

# Chỉ test Single-stage
powershell -ExecutionPolicy Bypass -File .\run-docker-attack-tests.ps1 -Profile single

# Chỉ test Multi-stage
powershell -ExecutionPolicy Bypass -File .\run-docker-attack-tests.ps1 -Profile multi

# Test + quét CVE (nếu có Trivy/Docker Scout)
powershell -ExecutionPolicy Bypass -File .\run-docker-attack-tests.ps1 -Profile all -RunScanner
```

### Bước 3: Xem kết quả

Báo cáo được tạo tự động trong:

```text
security-tests/reports/security-comparison-YYYYMMDD-HHmmss.md
```

## Kết quả mới nhất (24/06/2026)

| Chỉ số | Single-stage (thuần túy) | Multi-stage hardened |
|--------|:-----------------------:|:--------------------:|
| **PASS** | 3/20 (15%) | **13/20 (65%)** |
| **WARN** | 10/20 (50%) | 6/20 (30%) |
| **FAIL** | 6/20 (30%) | 0/20 (0%) |
| **Kích thước Web** | ~1.93 GB | ~408 MB |
| **Còn SDK?** | ❌ Có | ✅ Không |
| **Còn source code?** | ❌ Có | ✅ Không |
| **Còn build tools?** | ❌ Có (gcc, make, git) | ✅ Không |

## Các trạng thái kết quả

| Trạng thái | Ý nghĩa |
|-----------|---------|
| `PASS` | Đạt yêu cầu bảo mật |
| `FAIL` | Có rủi ro rõ, nên sửa |
| `WARN` | Có rủi ro theo production, chấp nhận được trong demo/dev |
| `SKIP` | Không kiểm tra được (thiếu image/container/công cụ) |
| `INFO` | Thông tin hỗ trợ đánh giá |

## Các bằng chứng chính cho báo cáo NCKH

### Single-stage (thuần túy) — Các lỗ hổng lộ rõ

| Mã test | Phát hiện | Kết quả |
|---------|-----------|:------:|
| IMG-01 | Container chạy root (UID=0) | FAIL |
| IMG-02 | Runtime image còn .NET SDK | FAIL |
| IMG-03 | Source code (.cs, .csproj) trong image | FAIL |
| IMG-05 | Build tools (gcc, make, git) trong image | FAIL |
| IMG-06 | Ghi file trái phép không bị chặn | FAIL |
| RUN-01 | Container chạy bằng root | FAIL |

### Multi-stage hardened — Các lỗ hổng đã được khắc phục

| Mã test | Biện pháp | Kết quả |
|---------|-----------|:------:|
| IMG-01 | `USER gymbro` + `chown` | PASS |
| IMG-02 | Multi-stage: tách SDK build, aspnet runtime | PASS |
| IMG-03 | `COPY --from=build` chỉ copy artifact | PASS |
| IMG-05 | aspnet runtime không chứa build tools | PASS |
| IMG-06 | Non-root user không ghi được /app | PASS |
| RUN-01 | `Config.User='gymbro'` | PASS |
| RUN-03 | `cap_drop: ALL` | PASS |
| RUN-04 | `no-new-privileges:true` | PASS |
| RUN-05 | Memory limits (256-512MB) | PASS |
| IMG-09 | HEALTHCHECK với curl mỗi 30s | PASS |
| CMP-04 | Security hardening đầy đủ | PASS |

### Các vấn đề còn tồn tại (WARN)

| Mã test | Vấn đề | Ghi chú |
|---------|--------|---------|
| RUN-02 | `read_only: true` chưa bật | Migration EF Core cần ghi file |
| CMP-01 | Secret trong compose | Đã fix cho Multi (.env), Single giữ baseline |
| CMP-02 | Development mode | Cả 2 dùng Development |
| CMP-03 | Over-publish ports | Demo nên giữ nguyên |

## Ghi chú

- **Single-stage thuần túy** dùng `FROM dotnet/sdk:8.0` duy nhất (không tách runtime) → image ~1.3-2.4GB.
- **Multi-stage hardened** dùng `FROM dotnet/sdk:8.0 AS build` + `FROM dotnet/aspnet:8.0 AS runtime` → image ~340-440MB, có đầy đủ hardening.
- Cả hai compose đều dùng `ASPNETCORE_ENVIRONMENT=Development` và publish nhiều port nội bộ để thuận tiện demo/test.