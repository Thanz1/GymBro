# Giải thích chi tiết các phương thức tấn công trong `run-docker-attack-tests.ps1`

## 1. Tổng quan

File này giải thích từng **phương thức tấn công** (test case) trong script `run-docker-attack-tests.ps1` — chúng dùng để đánh giá mức độ bảo mật của Docker image, container và Docker Compose.

Các test đều là **mô phỏng an toàn**: không khai thác, không phá hoại, không tác động đến dữ liệu thật.

Mỗi test case thuộc về một trong **ba hàm tấn công chính**:

| # | Tên hàm tấn công | Dòng | Mục tiêu |
|---|-----------------|:----:|----------|
| 1 | `Test-StaticImage` | ~170 | Quét image tĩnh (không cần chạy container) |
| 2 | `Test-ContainerRuntime` | ~300 | Quét container đang chạy |
| 3 | `Test-ComposeSecurity` | ~368 | Quét file docker-compose.yml |

---

## 2. TẤN CÔNG 1: `Test-StaticImage` (Quét image tĩnh)

**Nguyên lý:** Attacker lấy được image từ registry hoặc máy build → chạy container tạm từ image đó → đọc nội dung image mà không cần xin phép.

### Các test case trong `Test-StaticImage`

| Test | Mã | Hành vi tấn công | Ý nghĩa bảo mật | Single | Multi |
|------|:--:|------------------|-----------------|:------:|:-----:|
| **Đo kích thước** | IMG-00 | Đọc kích thước image | Image càng lớn → chứa càng nhiều công cụ/thành phần không cần thiết → bề mặt tấn công lớn hơn | ~1.7GB | ~350MB |
| **Kiểm tra user root** | IMG-01 | Chạy `id -u` trong container tạm | Nếu UID=0 (root), attacker có thể ghi file, cài đặt, leo thang quyền | FAIL (root) | ✅ PASS (gymbro) |
| **Tìm SDK .NET** | IMG-02 | Chạy `dotnet --list-sdks` | SDK có nhiều công cụ (dotnet-ef, build, publish...) → attacker có thể build mã độc ngay trong container | FAIL (còn SDK) | ✅ PASS (hết SDK) |
| **Tìm source code** | IMG-03 | Chạy `find *.cs *.csproj *.sln` | Source code lộ → attacker hiểu kiến trúc, tìm điểm yếu, đọc logic đăng nhập | FAIL (còn source) | ✅ PASS (sạch) |
| **Tìm file debug** | IMG-04 | Chạy `find *.pdb *.Development.json` | File `.pdb` chứa thông tin debug, `appsettings.Development.json` chứa secret thật | WARN (còn pdb) | ✅ PASS (DebugSymbols=false) |
| **Tìm build tools** | IMG-05 | Chạy `which gcc make git` | Build tools → attacker biên dịch mã độc ngay trong container | FAIL (còn tools) | ✅ PASS (sạch) |
| **Ghi file trái phép** | IMG-06 | Thử `touch /app/attack_test` | Nếu user root, attacker có thể ghi đè file ứng dụng, cài backdoor | FAIL (ghi được) | ✅ PASS (bị chặn) |
| **Leak secret trong history** | IMG-07 | Quét `docker history --no-trunc` | Password/token trong RUN, ENV, ARG vẫn còn trong history ngay cả khi đã xóa | PASS (không secret) | PASS (không secret) |
| **Leak secret trong env** | IMG-08 | Quét `docker inspect .Config.Env` | Secret trong ENV có thể đọc được bằng inspect → không nên bake vào image | PASS (không secret) | PASS (không secret) |
| **Thiếu HEALTHCHECK** | IMG-09 | Đọc `docker inspect .Config.Healthcheck` | Không có healthcheck → container chết ứng dụng không được phát hiện/kill | WARN (thiếu) | ✅ PASS (có healthcheck) |
| **Quét CVE** | IMG-10 | Trivy/Docker Scout | Tìm lỗ hổng trong base image và packages | (tùy chọn) | (tùy chọn) |

### Kết quả kỳ vọng:
- **Single-stage**: FAIL ở IMG-01, IMG-02, IMG-03, IMG-05 (vì dùng SDK image). WARN ở IMG-09.
- **Multi-stage**: PASS hầu hết (non-root user, không SDK, không source, không tools, có HEALTHCHECK).

---

## 3. TẤN CÔNG 2: `Test-ContainerRuntime` (Quét container đang chạy)

**Nguyên lý:** Attacker có được shell trong container (do lỗ hổng ứng dụng) → kiểm tra mức độ giới hạn của container.

### Các test case trong `Test-ContainerRuntime`

| Test | Mã | Hành vi tấn công | Ý nghĩa bảo mật | Single | Multi |
|------|:--:|------------------|-----------------|:------:|:-----:|
| **User container** | RUN-01 | `docker inspect .Config.User` | User non-root hạn chế thiệt hại nếu attacker chiếm được shell | FAIL (root) | ✅ PASS (gymbro) |
| **Read-only filesystem** | RUN-02 | `docker inspect .HostConfig.ReadonlyRootfs` | Filesystem read-only → attacker không thể ghi file mới | WARN (không read-only) | WARN (không read-only) |
| **Capability drop** | RUN-03 | `docker inspect .HostConfig.CapDrop` | `cap_drop: ALL` xóa toàn bộ capabilities kernel → attacker không thể leo thang quyền | WARN (không drop) | ✅ PASS (có drop) |
| **no-new-privileges** | RUN-04 | `docker inspect .HostConfig.SecurityOpt` | Ngăn process con nhận quyền cao hơn process cha (setuid) | WARN (không có) | ✅ PASS (có) |
| **Memory limits** | RUN-05 | `docker inspect .HostConfig.Memory` | Giới hạn RAM ngăn một container chiếm hết tài nguyên host (DoS) | WARN (unlimited) | ✅ PASS (có limit) |
| **Port publish** | RUN-06 | `docker inspect .NetworkSettings.Ports` | Càng ít port public → càng giảm bề mặt tấn công mạng | INFO | INFO |

### Kết quả kỳ vọng:
- **Single-stage**: FAIL ở RUN-01, WARN phần lớn (không hardening runtime).
- **Multi-stage**: PASS ở RUN-01, RUN-03, RUN-04, RUN-05. WARN ở RUN-02 do không dùng `read_only`.

---

## 4. TẤN CÔNG 3: `Test-ComposeSecurity` (Quét file Docker Compose)

**Nguyên lý:** Attacker đọc được file `docker-compose.yml` (do leak repository, config map, CI/CD log...).

### Các test case trong `Test-ComposeSecurity`

| Test | Mã | Hành vi tấn công | Ý nghĩa bảo mật | Single | Multi |
|------|:--:|------------------|-----------------|:------:|:-----:|
| **Secret trong compose** | CMP-01 | Quét regex password/key/token trong file | Hard-code secret → attacker có mật khẩu database, JWT, SMTP | FAIL (có secret) | FAIL (có secret) |
| **Development mode** | CMP-02 | Kiểm tra ASPNETCORE_ENVIRONMENT | Dev mode bật debug, exception stack trace, Swagger → attacker có thêm thông tin | WARN (Development) | WARN (Development) |
| **Port nội bộ public** | CMP-03 | Đếm port SQL/RabbitMQ/API public | API và database expose ra host → attacker có thể truy cập trực tiếp | WARN (over-publish) | WARN (over-publish) |
| **Security hardening** | CMP-04 | Kiểm tra `cap_drop`, `no-new-privileges`, `memory limits` | Hardening giảm thiểu thiệt hại nếu container bị xâm nhập | FAIL (không có) | ✅ PASS (có đủ) |

### Kết quả kỳ vọng:
- **Single-stage**: FAIL ở CMP-01 và CMP-04.
- **Multi-stage**: PASS ở CMP-04, WARN ở CMP-01, CMP-02, CMP-03 (cả hai compose đều có secret hard-code và Dev mode).

---

## 5. Tổng hợp: Bảng so sánh Single vs Multi

| Mã | Tên test | Single | Multi | Ghi chú |
|:--:|----------|:-----:|:-----:|---------|
| IMG-00 | Image size | ~1.7GB | ~350MB | Multi nhỏ hơn ~5 lần |
| IMG-01 | Non-root user | ❌ FAIL | ✅ PASS | Multi có USER gymbro |
| IMG-02 | No .NET SDK | ❌ FAIL | ✅ PASS | Multi dùng aspnet image |
| IMG-03 | No source code | ❌ FAIL | ✅ PASS | Multi COPY --from=build |
| IMG-04 | No debug files | ⚠️ WARN | ✅ PASS | Multi: DebugSymbols=false |
| IMG-05 | No build tools | ❌ FAIL | ✅ PASS | SDK image có gcc/make/git |
| IMG-06 | Write blocked | ❌ FAIL | ✅ PASS | Non-root user + chown |
| IMG-07 | Secret history | ✅ PASS | ✅ PASS | Cả 2 đều sạch |
| IMG-08 | Secret env | ✅ PASS | ✅ PASS | Cả 2 đều sạch |
| IMG-09 | HEALTHCHECK | ⚠️ WARN | ✅ PASS | Multi có healthcheck |
| RUN-01 | Non-root runtime | ❌ FAIL | ✅ PASS | Config.User='gymbro' |
| RUN-02 | Read-only fs | ⚠️ WARN | ⚠️ WARN | Không dùng read_only (do migration cần ghi) |
| RUN-03 | Capability drop | ⚠️ WARN | ✅ PASS | Multi có cap_drop: ALL |
| RUN-04 | no-new-privileges | ⚠️ WARN | ✅ PASS | Multi có security_opt |
| RUN-05 | Memory limits | ⚠️ WARN | ✅ PASS | Multi có deploy.resources.limits.memory |
| RUN-06 | Published ports | ℹ️ INFO | ℹ️ INFO | Demo expose nhiều port |
| CMP-01 | Hard-coded secrets | ℹ️ INFO | ✅ PASS | Multi: .env, Single: baseline |
| CMP-02 | Dev mode | ⚠️ WARN | ⚠️ WARN | Cả 2 dùng Development |
| CMP-03 | Over-publish ports | ⚠️ WARN | ⚠️ WARN | SQL/RabbitMQ/API public |
| CMP-04 | Security hardening | ❌ FAIL | ✅ PASS | Single không có cap_drop/no-new-priv/memory |

---

## 6. Kết luận

**Multi-stage hardened** cải thiện đáng kể so với **Single-stage**:

1. **Image level (7/7 PASS)** — không SDK, source, tools, .pdb → bề mặt tấn công giảm rõ rệt.
2. **Runtime hardening (4/6 PASS)** — non-root user, cap_drop, no-new-privileges, memory limits.
3. **Compose security (3/4 PASS)** — .env, security hardening, (WARN: Dev mode, over-publish port).

Tổng cộng: **Multi-stage PASS 13/20 test** vs **Single-stage PASS 3/20 test** (chênh lệch rõ rệt chứng minh hiệu quả của multi-stage + hardening cho bảo mật Docker).
