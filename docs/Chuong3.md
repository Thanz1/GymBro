# CHƯƠNG 3. ĐỀ XUẤT MÔ HÌNH

Chương này trình bày mô hình đóng gói và triển khai container cho hệ thống thương mại điện tử GymBro trên nền tảng Docker. Trọng tâm của mô hình không chỉ là làm cho ứng dụng chạy được trong container, mà còn tạo ra một quy trình có thể so sánh, đánh giá và tái sử dụng cho các dự án ASP.NET Core có cấu trúc tương tự.

Qua quá trình phân tích mã nguồn, GymBro được tổ chức theo hướng nhiều dịch vụ (service-oriented), bao gồm:

- **Tầng giao diện:** `GymBro.Web` (Razor Pages MVC), `GymBro.Gateway` (Ocelot API Gateway).
- **Tầng dịch vụ:** `GymBro.Identity.API`, `GymBro.Product.API`, `GymBro.Order.API`.
- **Tầng dùng chung:** `GymBro.Contracts` (DTOs & Events), `GymBro.Core` (Entity models), `GymBro.Infrastructure` (DbContext & Repository), `GymBro.Service` (Service layer).
- **Hạ tầng:** SQL Server 2019, RabbitMQ (message broker).

Dự án hiện có hai cấu hình Docker để phục vụ mục đích so sánh trong nghiên cứu:

- **`docker-compose-single.yml`**: Chạy các `Dockerfile.single` — mô hình baseline single-stage thuần túy, dùng SDK image cho cả build và runtime, làm đối chứng.
- **`docker-compose-multistage.yml`**: Chạy các `Dockerfile.multistage` — mô hình multi-stage được tăng cường bảo mật (hardened), là mục tiêu chính của đề tài.

Vì vậy, mô hình nghiên cứu trong chương này gồm hai hướng:

- **Baseline single-stage:** Mô hình đối chứng thuần túy, dùng duy nhất `dotnet/sdk:8.0` cho cả build và runtime. Image kết quả chứa toàn bộ SDK (~1.7GB), source code (.cs, .csproj), build tools (gcc, make, git), chạy bằng quyền root và không có hardening. Mục đích là phơi bày rõ các rủi ro bảo mật khi Docker hóa ứng dụng .NET theo cách đơn giản nhất.
- **Multi-stage hardened:** Mô hình đề xuất, tách build/runtime bằng multi-stage, đồng thời bổ sung non-root user, HEALTHCHECK, capability drop, no-new-privileges, memory limits, loại bỏ debug symbols và tách secrets ra file `.env`.

## 3.1. Kiến trúc triển khai tổng thể

### 3.1.1. Kiến trúc triển khai của GymBro

Trong môi trường Docker, GymBro được đề xuất triển khai theo mô hình nhiều container. Mỗi thành phần chính của hệ thống được đóng gói thành một image riêng và chạy như một service độc lập trong Docker Compose.

Hình 3.1 mô tả kiến trúc triển khai tổng thể của hệ thống:

```
┌─────────────────────────────────────────────────────────────────────┐
│                        NGƯỜI DÙNG (Browser)                         │
└──────────────┬──────────────────────────────────────┬───────────────┘
               │                                      │
               ▼                                      ▼
   ┌───────────────────┐                 ┌───────────────────┐
   │   GymBro.Web      │                 │  GymBro.Gateway   │
   │ (ASP.NET MVC)     │                 │ (Ocelot Gateway)  │
   │ :5000/5001→:8080  │                 │ :8000/8001→:8080  │
   └───┬───────┬───────┘                 └───┬───────┬───────┘
       │       │                             │       │
       │       └─────────────┬───────────────┤       │
       │                     │               │       │
       ▼                     ▼               ▼       │
┌──────────────┐  ┌──────────────┐  ┌──────────────┐ │
│ Identity.API │  │  Product.API │  │   Order.API  │◄┘
│:7001/7011→   │  │:7002/7012→   │  │:7003/7013→   │
│    :8080     │  │    :8080     │  │    :8080     │
└───┬────┬─────┘  └──────┬───────┘  └──┬─────┬─────┘
    │    │               │              │     │
    │    └───────┐       │    ┌─────────┘     │
    │            │       │    │               │
    ▼            ▼       ▼    ▼               ▼
┌────────┐ ┌──────────┐ ┌──────────┐
│  SQL   │ │ RabbitMQ │ │  SQL     │
│ Server │ │ :5672/   │ │  Server  │
│:1433/  │ │  15672   │ │(Product) │
│:1434   │ │          │ │(Order)   │
└────────┘ └──────────┘ └──────────┘
```

Ghi chú: Cổng dạng `X/Y` thể hiện multi-stage/single-stage. Mỗi service sử dụng database riêng: `GymBro_Identity`, `GymBro_Product`, `GymBro_Order`.

Các thành phần trong mô hình được mô tả chi tiết trong Bảng 3.1:

**Bảng 3.1. Các service trong hệ thống GymBro**

| Thành phần | Vai trò | Cổng host (Multi) | Cổng host (Single) | Cổng container |
|-----------|--------|:-----------------:|:------------------:|:--------------:|
| `gymbro_web` | Giao diện MVC cho khách hàng và admin | `5000` | `5001` | `8080` |
| `gateway` | API Gateway định tuyến đến các API nội bộ | `8000` | `8001` | `8080` |
| `identity_api` | Đăng ký, đăng nhập, quản lý người dùng, SignalR chat | `7001` | `7011` | `8080` |
| `product_api` | Sản phẩm, danh mục, đánh giá, nhà cung cấp | `7002` | `7012` | `8080` |
| `order_api` | Giỏ hàng, đơn hàng, thanh toán, wishlist | `7003` | `7013` | `8080` |
| `db` | SQL Server 2019 | `1433` | `1434` | `1433` |
| `rabbitmq` | Message broker cho event bất đồng bộ | `5672, 15672` | `5673, 15673` | `5672, 15672` |

Hai file Compose được thiết kế để có thể chạy song song trên cùng một máy:

- `docker-compose-multistage.yml`: project name `gymbro_multi`, network `gymbro_network_multi`, image tag `:multi`.
- `docker-compose-single.yml`: project name `gymbro_single`, network `gymbro_network`, image tag `:single`.

Các container gọi nhau bằng tên service nội bộ (`db`, `rabbitmq`, `identity_api`, `product_api`, `order_api`). Cách này giúp ứng dụng không phụ thuộc vào địa chỉ IP tĩnh và phù hợp với cơ chế service discovery cơ bản của Docker Compose.

Ví dụ, connection string của các API không trỏ đến `localhost`, mà trỏ đến SQL Server bằng hostname nội bộ:

```
Server=db;Database=GymBro_Product;User Id=sa;Password=${SA_PASSWORD};TrustServerCertificate=True
```

Tương tự, ServiceUrls trong Web sử dụng tên service nội bộ:

```
ServiceUrls__ProductApi=http://product_api:8080
ServiceUrls__OrderApi=http://order_api:8080
ServiceUrls__IdentityApi=http://identity_api:8080
```

### 3.1.2. Nguyên tắc triển khai đề xuất

Mô hình triển khai được xây dựng trên các nguyên tắc trình bày trong Bảng 3.2:

**Bảng 3.2. Nguyên tắc triển khai**

| Nguyên tắc | Áp dụng trong GymBro |
|-----------|---------------------|
| Tách container theo chức năng | Web, Gateway, Identity, Product, Order, SQL Server, RabbitMQ chạy độc lập |
| Tách build-time khỏi runtime | SDK chỉ trong stage build, runtime dùng aspnet image |
| Cấu hình qua biến môi trường | Connection string, URL service, RabbitMQ host truyền từ Compose |
| Không nhúng secrets vào image | Mật khẩu truyền qua `-e` hoặc file `.env`, không có trong Dockerfile |
| Dùng network nội bộ | Các API và hạ tầng giao tiếp qua bridge network |
| Lưu dữ liệu bằng volume | SQL Server dùng volume để dữ liệu bền vững |
| Ưu tiên least privilege | Container ứng dụng chạy bằng user `gymbro`, không chạy `root` |
| Tự động migration database | Mỗi API tự chạy `Database.Migrate()` khi khởi động |

### 3.1.3. Công thức Docker hóa cho dự án ASP.NET Core tương tự

Từ trường hợp GymBro, có thể khái quát quy trình Docker hóa cho một dự án ASP.NET Core nhiều service:

```
Dự án .NET nhiều service
        │
        ▼
Xác định entry point chạy được (Web, API, Gateway...)
        │
        ▼
Tạo Dockerfile cho từng entry point (multi-stage: build → runtime)
        │
        ▼
Đưa dependency hạ tầng vào docker-compose (SQL, RabbitMQ...)
        │
        ▼
Khai báo network, volume, environment variables
        │
        ▼
Build → Test → Scan → Deploy
```

Công thức tổng quát cho một service:

```
System = AppServices + InfrastructureServices + Network + Volumes + Configuration
```

Trong đó:
- **AppServices**: Các service do nhóm phát triển.
- **InfrastructureServices**: SQL Server, RabbitMQ, Redis...
- **Network**: Bridge network để container gọi nhau bằng tên service.
- **Volumes**: Lưu dữ liệu bền vững.
- **Configuration**: Biến môi trường, file `.env`.

Quy tắc thực hành:
1. Mỗi project chạy độc lập bằng `dotnet run` nên có Dockerfile riêng.
2. Project thư viện (`Core`, `Contracts`, `Infrastructure`, `Service`) không cần container riêng, nhưng phải copy `.csproj` trong stage restore.
3. Dùng tên service trong Compose thay vì `localhost`.
4. Không hard-code mật khẩu trong Dockerfile.
5. Có `.dockerignore` để loại `bin`, `obj`, `.git`, `.vs`.
6. Với production, dùng `.env` hoặc Docker secrets.

## 3.2. Mô hình Baseline Single-stage

### 3.2.1. Mục đích của mô hình baseline

Trong dự án GymBro, mô hình baseline single-stage được triển khai bằng các file `Dockerfile.single` tại từng service: `GymBro.Web`, `GymBro.Gateway`, `GymBro.Identity.API`, `GymBro.Product.API` và `GymBro.Order.API`. Các Dockerfile này sử dụng duy nhất `FROM dotnet/sdk:8.0` cho cả build và runtime — không tách stage, không dùng ASP.NET runtime image.

Mục đích của baseline là phơi bày rõ các vấn đề bảo mật khi Docker hóa ứng dụng .NET theo cách đơn giản nhất:

- Image runtime chứa toàn bộ .NET SDK (~1.7GB) — attacker có thể biên dịch mã độc ngay trong container.
- File source code (.cs, .csproj, .sln) vẫn tồn tại trong image cuối — attacker đọc được toàn bộ logic ứng dụng.
- Build tools (gcc, make, git, dotnet-ef) có sẵn — attacker có thể clone repo, biên dịch công cụ tấn công.
- Container chạy bằng user `root` mặc định — attacker có toàn quyền trong container nếu chiếm được shell.
- Không có HEALTHCHECK, capability drop, no-new-privileges, memory limits.
- Secrets bị hard-code trực tiếp trong file docker-compose.

Trong báo cáo này, `GymBro.Web` được dùng làm ví dụ đại diện. Các service còn lại dùng cùng công thức, chỉ thay project publish và DLL entrypoint.

### 3.2.2. Dockerfile baseline trong dự án

Dockerfile.single tại mỗi service tuân theo mẫu chung — chỉ dùng một image SDK duy nhất, không có stage runtime riêng:

```dockerfile
# Baseline Single-stage (THUẦN TÚY) — SDK image cho cả build và runtime
FROM mcr.microsoft.com/dotnet/sdk:8.0
WORKDIR /src

COPY . .
WORKDIR /src/GymBro.Web

RUN dotnet restore "GymBro.Web.csproj"
RUN dotnet publish "GymBro.Web.csproj" -c Release -o /app/publish \
    /p:UseAppHost=false /p:ErrorOnDuplicatePublishOutputFiles=false

WORKDIR /app/publish
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "GymBro.Web.dll"]
```

Đây là single-stage đúng nghĩa: không có `FROM ... AS runtime`, không có `COPY --from=build`. Mọi thứ — SDK, source code, build tools — đều nằm trong image cuối cùng. Mô hình này được giữ làm baseline để so sánh với multi-stage hardened.

### 3.2.3. Phân tích baseline

**Bảng 3.3. Đặc điểm của mô hình baseline single-stage**

| Tiêu chí | Đặc điểm | Đánh giá |
|---------|----------|----------|
| Base image | `mcr.microsoft.com/dotnet/sdk:8.0` (duy nhất) | Chứa SDK, source, build tools |
| Số stage | 1 (không tách build/runtime) | Toàn bộ trong cùng image |
| SDK trong image | ✅ Còn (có thể build mã độc) | Rủi ro bảo mật rất cao |
| Source code trong image | ✅ Còn (.cs, .csproj) | Lộ toàn bộ logic ứng dụng |
| Build tools | ✅ Còn (gcc, make, git, dotnet-ef) | Attacker có thể biên dịch công cụ |
| User runtime | Mặc định (`root`) | Rủi ro bảo mật cao |
| HEALTHCHECK | Không có | Không phát hiện container treo |
| Capability drop | Không có | Container có 14 capabilities mặc định |
| no-new-privileges | Không có | Có thể leo thang setuid |
| Memory limits | Không có (trừ SQL Server 1024M) | Rủi ro DoS |
| Debug symbols | Còn `.pdb` | Rò rỉ thông tin debug |
| Secrets | Hard-code trong compose | Rủi ro rò rỉ khi commit |
| Kích thước image | ~1.3-2.4 GB | Chứa toàn bộ SDK + source |

Baseline trong dự án được dùng làm mô hình đối chứng, không nên dùng làm phương án triển khai cuối cùng.

## 3.3. Mô hình Docker Multi-stage Hardened

### 3.3.1. Ý tưởng của mô hình đề xuất

Multi-stage Hardened là mô hình mở rộng từ multi-stage build truyền thống, bổ sung các biện pháp bảo mật ở cả ba cấp: image, container runtime và Docker Compose. Trong dự án GymBro, mô hình này được triển khai qua các file `Dockerfile.multistage` và `docker-compose-multistage.yml`.

Các cải tiến chính so với baseline:

| Cấp | Biện pháp | Tác dụng bảo mật |
|-----|----------|-----------------|
| **Image** | Multi-stage build | Loại bỏ SDK, source code, build tools khỏi runtime |
| **Image** | `USER gymbro` | Container không chạy bằng root |
| **Image** | `HEALTHCHECK` | Docker tự phát hiện và restart container lỗi |
| **Image** | `/p:DebugSymbols=false /p:DebugType=none` | Không tạo file `.pdb` |
| **Compose** | `cap_drop: ALL` + `cap_add: NET_BIND_SERVICE` | Xóa capabilities kernel dư thừa |
| **Compose** | `security_opt: no-new-privileges:true` | Ngăn leo thang setuid |
| **Compose** | `deploy.resources.limits.memory` | Giới hạn RAM, chống DoS |
| **Compose** | `.env` cho secrets | Tách mật khẩu khỏi file compose |

### 3.3.2. Dockerfile.multistage cho GymBro.Web

Dưới đây là Dockerfile.multistage thực tế cho `GymBro.Web`:

```dockerfile
# ============================================================
# STAGE 1: Build
# ============================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy .csproj files để tận dụng Docker layer cache
COPY GymBro.Core/GymBro.Core.csproj GymBro.Core/
COPY GymBro.Contracts/GymBro.Contracts.csproj GymBro.Contracts/
COPY GymBro.Infrastructure/GymBro.Infrastructure.csproj GymBro.Infrastructure/
COPY GymBro.Service/GymBro.Service.csproj GymBro.Service/
COPY GymBro.Web/GymBro.Web.csproj GymBro.Web/

RUN dotnet restore "GymBro.Web/GymBro.Web.csproj"

COPY . .
WORKDIR /src/GymBro.Web
RUN dotnet publish "GymBro.Web.csproj" -c Release -o /app/publish \
    /p:UseAppHost=false \
    /p:ErrorOnDuplicatePublishOutputFiles=false \
    /p:DebugSymbols=false \
    /p:DebugType=none

# ============================================================
# STAGE 2: Runtime (Hardened)
# ============================================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Cài curl cho HEALTHCHECK
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Tạo non-root user
RUN groupadd -r gymbro && useradd -r -g gymbro -d /home/gymbro -m gymbro

COPY --from=build /app/publish .
RUN chown -R gymbro:gymbro /app /home/gymbro

USER gymbro

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
    CMD curl -f http://localhost:8080/ || exit 1

ENTRYPOINT ["dotnet", "GymBro.Web.dll"]
```

### 3.3.3. Dockerfile.multistage cho các API

Các API như `GymBro.Identity.API`, `GymBro.Product.API`, `GymBro.Order.API` và `GymBro.Gateway` sử dụng cùng công thức, chỉ khác ở danh sách `.csproj` cần copy trong stage restore và DLL entrypoint.

**Bảng 3.4. Tham số Dockerfile cho từng service**

| Service | Project publish | DLL entrypoint | Dependencies .csproj cần copy |
|--------|----------------|----------------|-------------------------------|
| `gateway` | `GymBro.Gateway/GymBro.Gateway.csproj` | `GymBro.Gateway.dll` | Chỉ Gateway (không có ProjectReference) |
| `identity_api` | `GymBro.Identity.API/GymBro.Identity.API.csproj` | `GymBro.Identity.API.dll` | Core, Contracts, Infrastructure, Service, Order, Product, Web |
| `product_api` | `GymBro.Product.API/GymBro.Product.API.csproj` | `GymBro.Product.API.dll` | Core, Contracts, Infrastructure, Service, Order |
| `order_api` | `GymBro.Order.API/GymBro.Order.API.csproj` | `GymBro.Order.API.dll` | Core, Contracts, Infrastructure, Service |
| `gymbro_web` | `GymBro.Web/GymBro.Web.csproj` | `GymBro.Web.dll` | Core, Contracts, Infrastructure, Service |

### 3.3.4. Hardening ở mức Docker Compose

Dockerfile an toàn hơn là chưa đủ. Khi chạy container, Compose cũng cần giới hạn quyền và tách cấu hình nhạy cảm. Dưới đây là cấu hình hardening áp dụng trong `docker-compose-multistage.yml`:

```yaml
# docker-compose-multistage.yml (trích đoạn)
services:
  identity_api:
    build:
      context: .
      dockerfile: GymBro.Identity.API/Dockerfile.multistage
    image: gymbro-identity:multi
    restart: always
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Server=db;Database=GymBro_Identity;User Id=sa;Password=${SA_PASSWORD};TrustServerCertificate=True
      - RabbitMQ__HostName=rabbitmq
    ports:
      - "7001:8080"
    cap_drop:
      - ALL
    cap_add:
      - NET_BIND_SERVICE
    security_opt:
      - no-new-privileges:true
    deploy:
      resources:
        limits:
          memory: 512M
```

Các biến mật khẩu được tách riêng vào file `.env`:

```env
# .env
SA_PASSWORD=GymBro@2024Password
RABBITMQ_USER=guest
RABBITMQ_PASS=guest
```

Đối chiếu với single-stage baseline, multistage đã cải thiện đáng kể ở cấp Compose:

**Bảng 3.5. So sánh Compose hardening**

| Tiêu chí | docker-compose-single.yml | docker-compose-multistage.yml |
|---------|:-------------------------:|:-----------------------------:|
| cap_drop: ALL | ❌ Không có | ✅ Có |
| no-new-privileges | ❌ Không có | ✅ Có |
| Memory limits | ❌ Không có (trừ DB) | ✅ Có (API: 512M, Gateway: 256M, DB: 2048M) |
| Secrets | Hard-code trong file | `${SA_PASSWORD}` từ `.env` |
| Môi trường | Development | Development |

### 3.3.5. So sánh baseline và multi-stage hardened

**Bảng 3.6. So sánh tổng thể hai mô hình**

| Tiêu chí | Baseline single-stage | Multi-stage hardened |
|---------|:---------------------:|:--------------------:|
| Số stage | 1 (SDK image duy nhất) | 2 (build + runtime hardened) |
| Image runtime | SDK image (~1.7GB base) | ASP.NET runtime (~200MB base) |
| SDK trong image cuối | ✅ Có | ❌ Không |
| Source code trong image cuối | ✅ Có (.cs, .csproj) | ❌ Không |
| Build tools trong image | ✅ Có (gcc, make, git) | ❌ Không |
| Debug symbols (.pdb) | Còn | Không (`DebugSymbols=false`) |
| User runtime | `root` | `gymbro` (non-root) |
| HEALTHCHECK | Không | Có (curl) |
| Capability drop | Không | `cap_drop: ALL` |
| no-new-privileges | Không | Có |
| Memory limits | Không | Có |
| Secrets trong compose | Hard-code | `.env` |

Như vậy, Multi-stage Hardened không chỉ là kỹ thuật giảm dung lượng image. Trong phạm vi đề tài, nó được xem là mô hình tăng cường bảo mật container toàn diện: giảm thành phần thừa, giảm quyền runtime, ngăn leo thang đặc quyền, hạn chế rò rỉ cấu hình nhạy cảm và tạo điều kiện cho quy trình kiểm thử bảo mật tự động.

