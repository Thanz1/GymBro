# CHƯƠNG 4. THỰC NGHIỆM VÀ ĐÁNH GIÁ

Chương này trình bày quá trình thực nghiệm so sánh hai mô hình Docker đã đề xuất trong Chương 3 trên hệ thống GymBro: mô hình Baseline Single-stage và mô hình Multi-stage Hardened. Mục tiêu là kiểm chứng hiệu quả của các biện pháp tăng cường bảo mật thông qua số liệu định lượng.

## 4.1. Mục tiêu thực nghiệm

Thực nghiệm nhằm trả lời ba câu hỏi nghiên cứu:

- **RQ1:** Multi-stage hardened giảm được những thành phần gây rủi ro nào trong image so với single-stage?
- **RQ2:** Các biện pháp hardening (USER, HEALTHCHECK, cap_drop, no-new-privileges, memory limits, .env) có cải thiện được điểm bảo mật khi kiểm thử không?
- **RQ3:** Hệ thống có hoạt động bình thường sau khi áp dụng hardening không?

## 4.2. Phương pháp thực nghiệm

### 4.2.1. Môi trường

| Thông số | Giá trị |
|---------|--------|
| Hệ điều hành | Windows 10, Docker Desktop với WSL2 |
| .NET Runtime | .NET 8 (ASP.NET Core 8.0) |
| Công cụ đo | `docker images`, `docker inspect` |
| Script kiểm thử | `security-tests/run-docker-attack-tests.ps1` |
| Thời điểm thực nghiệm | Tháng 6/2026 |

### 4.2.2. Hai mô hình được so sánh

| Đặc điểm | Baseline Single-stage | Multi-stage Hardened |
|----------|:---------------------:|:--------------------:|
| File compose | `docker-compose-single.yml` | `docker-compose-multistage.yml` |
| Dockerfile | `Dockerfile.single` (1 stage, SDK image duy nhất) | `Dockerfile.multistage` (2 stage, hardened) |
| Base image runtime | `dotnet/sdk:8.0` | `dotnet/aspnet:8.0` |

### 4.2.3. Quy trình thực hiện

1. Build cả hai mô hình bằng `docker compose build`
2. Khởi chạy container bằng `docker compose up -d`
3. Smoke test các endpoint chính
4. Chạy script `run-docker-attack-tests.ps1 -Profile all`
5. Ghi nhận kích thước image và kết quả kiểm thử

## 4.3. Kết quả

### 4.3.1. Kích thước image

Kết quả đo bằng lệnh `docker images`:

| Service | Single-stage | Multi-stage | Tỉ lệ giảm |
|--------|:-----------:|:-----------:|:----------:|
| Gateway | 1.33 GB | 342 MB | 74.3% |
| Web | 1.93 GB | 408 MB | 78.9% |
| Identity API | 2.41 GB | 442 MB | 81.7% |
| Product API | 2.10 GB | 390 MB | 81.4% |
| Order API | 2.03 GB | 390 MB | 80.8% |
| **Trung bình** | **1.96 GB** | **394 MB** | **79.9%** |

**Nhận xét:** Multi-stage hardened có kích thước nhỏ hơn trung bình 5 lần (giảm ~80%) so với single-stage. Nguyên nhân: single-stage dùng SDK image (~1.7GB base), trong khi multi-stage chỉ dùng ASP.NET runtime (~200MB base). Chênh lệch ~40MB ở multi-stage so với aspnet thuần đến từ việc cài thêm `curl` cho HEALTHCHECK và tạo user `gymbro`.

### 4.3.2. Kết quả kiểm thử bảo mật

Script `run-docker-attack-tests.ps1` tự động kiểm tra 20 tiêu chí bảo mật chia thành 3 nhóm: image tĩnh (IMG-xx), container runtime (RUN-xx), và Docker Compose (CMP-xx). Mỗi tiêu chí được đánh giá: PASS (đạt), FAIL (có rủi ro), WARN (cảnh báo).

#### Nhóm IMG — Kiểm tra image tĩnh

| Mã | Tiêu chí | Single-stage | Multi-stage |
|:--:|----------|:-----------:|:-----------:|
| IMG-01 | User không phải root | ❌ FAIL (uid=0) | ✅ PASS (gymbro) |
| IMG-02 | Không còn .NET SDK | ❌ FAIL (còn SDK 8.0) | ✅ PASS |
| IMG-03 | Không leak source code | ❌ FAIL (còn .cs, .csproj) | ✅ PASS |
| IMG-04 | Không có debug file | ⚠️ WARN | ✅ PASS |
| IMG-05 | Không có build tools | ❌ FAIL (còn gcc, make, git) | ✅ PASS |
| IMG-06 | Ghi file bị chặn | ❌ FAIL | ✅ PASS |
| IMG-09 | Có HEALTHCHECK | ⚠️ WARN | ✅ PASS |

#### Nhóm RUN — Kiểm tra container runtime

| Mã | Tiêu chí | Single-stage | Multi-stage |
|:--:|----------|:-----------:|:-----------:|
| RUN-01 | Container user non-root | ❌ FAIL | ✅ PASS (gymbro) |
| RUN-03 | Capabilities bị drop | ⚠️ WARN | ✅ PASS (cap_drop: ALL) |
| RUN-04 | no-new-privileges | ⚠️ WARN | ✅ PASS |
| RUN-05 | Memory limits | ⚠️ WARN | ✅ PASS (256-512MB) |

#### Nhóm CMP — Kiểm tra Docker Compose

| Mã | Tiêu chí | Single-stage | Multi-stage |
|:--:|----------|:-----------:|:-----------:|
| CMP-01 | Không hard-code secrets | ℹ️ INFO | ✅ PASS (.env) |
| CMP-04 | Security hardening | ❌ FAIL | ✅ PASS |

#### Tổng hợp

**Bảng 4.1. Tổng hợp kết quả kiểm thử**

| Chỉ số | Single-stage | Multi-stage | Cải thiện |
|--------|:-----------:|:-----------:|:---------:|
| PASS | 3/20 (15%) | 13/20 (65%) | +10 |
| WARN | 10/20 (50%) | 6/20 (30%) | -4 |
| FAIL | 6/20 (30%) | 0/20 (0%) | -6 |

Các tiêu chí WARN còn tồn tại ở cả hai mô hình (RUN-02: read_only chưa bật, CMP-02: Development mode, CMP-03: over-publish ports) là những hạn chế có chủ đích để thuận tiện cho việc phát triển và kiểm thử.

### 4.3.3. Kết quả chạy hệ thống

Cả hai mô hình đều khởi động thành công 7 container (SQL Server, RabbitMQ, Identity API, Product API, Order API, Gateway, Web). Smoke test cho Web và các API đều phản hồi HTTP 200. Database được tạo tự động, migration chạy đúng, seed 3 tài khoản mặc định.

**Kết luận:** Việc áp dụng hardening không làm ảnh hưởng đến chức năng của hệ thống.

## 4.4. Đánh giá

### RQ1: Multi-stage hardened giảm được những thành phần gây rủi ro nào?

Kết quả nhóm IMG cho thấy multi-stage hardened đã loại bỏ hoàn toàn:

- **.NET SDK:** Không còn SDK trong runtime image (IMG-02: FAIL → PASS). Single-stage để lại toàn bộ SDK, cho phép attacker biên dịch mã độc ngay trong container.
- **Source code:** Không còn file .cs, .csproj trong image (IMG-03: FAIL → PASS). Single-stage giữ nguyên toàn bộ source code, làm lộ logic ứng dụng.
- **Build tools:** Không còn gcc, make, git (IMG-05: FAIL → PASS). Single-stage giữ lại toàn bộ công cụ build.

Ngoài ra, kích thước image giảm trung bình 79.9% (từ 1.96GB xuống 394MB).

### RQ2: Các biện pháp hardening có cải thiện điểm bảo mật không?

Có. Kết quả nhóm RUN và CMP cho thấy:

- **Non-root user:** Từ FAIL → PASS ở cả IMG-01 và RUN-01.
- **Capability drop:** Từ WARN → PASS (RUN-03).
- **no-new-privileges:** Từ WARN → PASS (RUN-04).
- **Memory limits:** Từ WARN → PASS (RUN-05).
- **Secrets tách riêng:** Từ INFO → PASS (CMP-01).
- **Compose hardening:** Từ FAIL → PASS (CMP-04).

Tổng thể, multi-stage đạt 13/20 PASS so với 3/20 PASS của single-stage — cải thiện 333%.

### RQ3: Hệ thống có hoạt động bình thường sau hardening không?

Có. Tất cả container khởi động thành công, Web và API phản hồi HTTP 200, database migration và seed hoạt động đúng. Các biện pháp hardening được áp dụng không gây gián đoạn chức năng.

## 4.5. Hạn chế

- Script `run-docker-attack-tests.ps1` chưa chạy quét CVE (Trivy/Docker Scout) do chưa cài đặt công cụ.
- `read_only: true` chưa được bật do EF Core Migration cần ghi file tạm.
- Cả hai mô hình đều dùng Development mode và publish nhiều port — phù hợp cho demo nhưng cần điều chỉnh khi triển khai production.

## 4.6. Kết luận chương

Thực nghiệm trên hệ thống GymBro đã chứng minh hiệu quả của mô hình Docker Multi-stage Hardened:

1. **Kích thước image giảm ~80%** (1.96GB → 394MB) nhờ tách SDK khỏi runtime.
2. **Loại bỏ SDK, source code, build tools** khỏi runtime image — các thành phần nguy hiểm nhất nếu bị attacker khai thác.
3. **Cải thiện 10 tiêu chí bảo mật** (từ 3/20 lên 13/20 PASS), không còn tiêu chí FAIL.
4. **Hệ thống hoạt động bình thường** sau khi áp dụng toàn bộ hardening.

Kết quả này khẳng định mô hình Multi-stage Hardened phù hợp và hiệu quả cho việc tăng cường bảo mật container trong các ứng dụng ASP.NET Core.