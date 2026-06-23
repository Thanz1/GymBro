# Cần làm sau khi clone dự án GymBro về máy

File này hướng dẫn các bước cần thực hiện ngay sau khi clone dự án từ GitHub về máy cá nhân, để đưa dự án vào trạng thái sẵn sàng chạy giống như tiến độ hiện tại của nhóm nghiên cứu.

---

## 1. Yêu cầu hệ thống

| Công cụ | Phiên bản tối thiểu | Kiểm tra |
|---------|:------------------:|----------|
| **Docker Desktop** | 24.0+ | `docker --version` |
| **Docker Compose** | v2.20+ (đi kèm Docker Desktop) | `docker compose version` |
| **.NET SDK** (chỉ cần nếu dev) | 8.0 | `dotnet --version` |
| **Git** | Bất kỳ | `git --version` |

> **Lưu ý:** Bạn **không cần cài .NET SDK** nếu chỉ muốn chạy dự án. Docker sẽ tự build và chạy toàn bộ.

---

## 2. Clone dự án

```powershell
git clone https://github.com/Thanz1/GymBro.git
cd GymBro
```

---

## 3. Thiết lập file `.env` cho Docker Compose (QUAN TRỌNG)

File `.env` chứa mật khẩu cho SQL Server và RabbitMQ, dùng bởi `docker-compose-multistage.yml`. File này **bị gitignore chặn** (không được commit lên GitHub để bảo vệ secret), nên sau khi clone về bạn cần tự tạo:

```powershell
# Copy file template .env.example thành .env
copy .env.example .env
```

Nội dung file `.env` sau khi copy (có sẵn giá trị mặc định, không cần sửa):
```env
SA_PASSWORD=GymBro@2024Password
RABBITMQ_USER=guest
RABBITMQ_PASS=guest
```

---

## 4. Hai mô hình Docker trong dự án

Dự án có 2 cấu hình Docker để phục vụ nghiên cứu so sánh:

| Đặc điểm | Single-stage (thuần túy) | Multi-stage hardened |
|----------|:------------------------:|:--------------------:|
| File compose | `docker-compose-single.yml` | `docker-compose-multistage.yml` |
| Dockerfile | `Dockerfile.single` | `Dockerfile.multistage` |
| Base image | `dotnet/sdk:8.0` (SDK ~1.7GB) | `dotnet/sdk:8.0` build + `dotnet/aspnet:8.0` runtime |
| Kích thước image | **~1.3-2.4 GB** | **~340-440 MB** |
| Còn .NET SDK? | ✅ Có | ❌ Không |
| Còn source code? | ✅ Có (.cs, .csproj) | ❌ Không |
| Còn build tools? | ✅ Có (gcc, make, git) | ❌ Không |
| User runtime | root | `gymbro` (non-root) |
| HEALTHCHECK | Không | Có (curl mỗi 30s) |
| Debug symbols | Còn (.pdb) | Đã xóa (DebugSymbols=false) |
| Capability drop | Không | `cap_drop: ALL` |
| no-new-privileges | Không | Có |
| Memory limits | Không | Có (256-512MB) |
| Secrets | Hard-code trong compose | `${SA_PASSWORD}` từ `.env` |

---

## 5. Chạy dự án (chọn 1 trong 2 cách)

### Cách A: Chạy Single-stage thuần túy (baseline, đơn giản nhất)

```powershell
docker compose -f docker-compose-single.yml up -d --build
```

**Port sau khi chạy:**

| Service | URL |
|---------|-----|
| **Web App** | http://localhost:5001 |
| **Identity API Swagger** | http://localhost:7011/swagger |
| **Product API Swagger** | http://localhost:7012/swagger |
| **Order API Swagger** | http://localhost:7013/swagger |
| **RabbitMQ Admin** | http://localhost:15673 (guest/guest) |
| **SQL Server** | localhost:1434 (sa / GymBro@2024Password) |

### Cách B: Chạy Multi-stage hardened (bảo mật, cần làm bước 3 trước)

```powershell
docker compose -f docker-compose-multistage.yml up -d --build
```

**Port sau khi chạy:**

| Service | URL |
|---------|-----|
| **Web App** | http://localhost:5000 |
| **Identity API Swagger** | http://localhost:7001/swagger |
| **Product API Swagger** | http://localhost:7002/swagger |
| **Order API Swagger** | http://localhost:7003/swagger |
| **Gateway** | http://localhost:8000 |
| **RabbitMQ Admin** | http://localhost:15672 (guest/guest) |
| **SQL Server** | localhost:1433 (sa / GymBro@2024Password) |

> **Khuyên dùng Cách A** cho người mới — không cần file `.env`, chạy được ngay. Cách B dành cho mục đích nghiên cứu bảo mật.

---

## 6. Tài khoản đăng nhập mặc định

Sau khi chạy lần đầu, database sẽ tự động được tạo và seed sẵn 3 tài khoản:

| Username | Password | Vai trò |
|----------|----------|---------|
| **admin** | `Admin@123` | Quản trị viên (đầy đủ quyền CRUD) |
| user | `User@123` | Khách hàng |
| staff | `Staff@123` | Nhân viên |

Đăng nhập tại: **http://localhost:5001/Account/Login** (Single) hoặc **http://localhost:5000/Account/Login** (Multi)

---

## 7. Kiểm tra hệ thống đã chạy đúng chưa

```powershell
# Kiểm tra tất cả container đang chạy
docker ps --filter "name=gymbro" --format "table {{.Names}}\t{{.Status}}"

# Kiểm tra Web có phản hồi không
curl http://localhost:5001

# Kiểm tra Identity API
curl http://localhost:7011/swagger/index.html

# Kiểm tra Product API
curl http://localhost:7012/api/product
```

Kết quả mong đợi: **7 container** đều ở trạng thái `Up`.

---

## 8. Chạy script kiểm tra bảo mật (dành cho nghiên cứu)

```powershell
cd security-tests

# Chạy test so sánh cả Single và Multi (khuyên dùng)
powershell -ExecutionPolicy Bypass -File .\run-docker-attack-tests.ps1 -Profile all

# Chỉ test Multi-stage
powershell -ExecutionPolicy Bypass -File .\run-docker-attack-tests.ps1 -Profile multi

# Chỉ test Single-stage
powershell -ExecutionPolicy Bypass -File .\run-docker-attack-tests.ps1 -Profile single
```

Báo cáo được lưu trong: `security-tests/reports/security-comparison-*.md`

Kết quả kỳ vọng:

| Chỉ số | Single-stage | Multi-stage |
|--------|:-----------:|:-----------:|
| PASS | 3/20 (15%) | 13/20 (65%) |
| WARN | 10/20 (50%) | 6/20 (30%) |
| FAIL | 6/20 (30%) | 0/20 (0%) |

---

## 9. Dừng hệ thống

```powershell
# Dừng single-stage
docker compose -f docker-compose-single.yml down

# Dừng multi-stage
docker compose -f docker-compose-multistage.yml down

# Dừng và xóa toàn bộ dữ liệu (database sẽ mất)
docker compose -f docker-compose-single.yml down -v
docker compose -f docker-compose-multistage.yml down -v
```

---

## 10. Cấu trúc dự án sau khi clone

```
GymBro/
├── .env                          ← Bạn tự tạo từ .env.example (bước 3)
├── .env.example                  ← Template mẫu (có sẵn, được commit)
├── docker-compose-single.yml     ← Single-stage thuần túy (SDK image)
├── docker-compose-multistage.yml ← Multi-stage hardened (cần .env)
├── CAN_LAM_SAU_KHI_CLONE.md      ← File này
├── GymBro.Gateway/
│   ├── Dockerfile.single         ← Single-stage (SDK image)
│   └── Dockerfile.multistage     ← Multi-stage hardened
├── GymBro.Identity.API/
│   ├── Dockerfile.single
│   └── Dockerfile.multistage
├── GymBro.Product.API/
│   ├── Dockerfile.single
│   └── Dockerfile.multistage
├── GymBro.Order.API/
│   ├── Dockerfile.single
│   └── Dockerfile.multistage
├── GymBro.Web/
│   ├── Dockerfile.single
│   └── Dockerfile.multistage
├── GymBro.Core/                  ← Entity models
├── GymBro.Contracts/             ← DTOs & Events
├── GymBro.Infrastructure/        ← DbContext & Repository
├── GymBro.Service/               ← Service layer (gọi API)
├── docs/                         ← Báo cáo NCKH (Chương 3, Chương 4)
│   ├── Chuong3.md
│   └── Chuong4.md
└── security-tests/               ← Script tấn công & báo cáo bảo mật
    ├── run-docker-attack-tests.ps1
    ├── GIAI_THICH_TAN_CONG_DOCKER.md
    ├── GIAI_THICH_PHUONG_THUC_TAN_CONG.md
    ├── GHI_CHU_CAC_FILE_TEST_BAO_MAT.md
    ├── README.md
    └── reports/
```

---

## 11. Xử lý sự cố thường gặp

| Lỗi | Nguyên nhân | Cách sửa |
|-----|------------|----------|
| **Port đã được sử dụng** | Single và Multi chạy cùng lúc | Chỉ chạy 1 trong 2, hoặc tắt bản kia |
| **SQL Server không khởi động** | Docker chưa cấp đủ RAM (cần ≥4GB) | Tăng RAM cho Docker Desktop: Settings → Resources → Memory |
| **Web không load được** | Container chưa khởi động xong | Đợi thêm 30 giây rồi thử lại |
| `docker compose` không nhận diện | Docker Compose cũ (v1) | Dùng `docker-compose` (có dấu gạch ngang) hoặc cập nhật Docker Desktop |
| **Không đăng nhập được** | Migration chưa chạy xong | `docker logs gymbro_identity_api_single` để kiểm tra log |
| **File .env không tồn tại** | Chưa làm bước 3 | `copy .env.example .env` |
| **Single-stage image quá lớn?** | Bình thường — SDK image ~1.7GB | Đây là đặc điểm của baseline, dùng để so sánh với Multi-stage |