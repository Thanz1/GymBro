# BÁO CÁO NGHIÊN CỨU

**Đề tài:** Ứng dụng kỹ thuật Docker Multi-stage Build trong tối ưu hóa và tăng cường bảo mật khi triển khai website bán hàng GymBro

## CHƯƠNG 1. GIỚI THIỆU TỔNG QUAN

### 1.1. Lý do chọn đề tài

Trong phát triển phần mềm hiện đại, nhu cầu triển khai ứng dụng nhanh, đồng nhất môi trường và dễ mở rộng ngày càng trở nên quan trọng. Docker và công nghệ container hóa cho phép đóng gói toàn bộ ứng dụng cùng các thành phần phụ thuộc vào một image thống nhất, từ đó giúp rút ngắn thời gian triển khai và giảm sai khác giữa môi trường phát triển, kiểm thử và vận hành thực tế.

Tuy nhiên, khi áp dụng Docker trong các hệ thống web thực tế, một vấn đề phổ biến là image được tạo ra thường có kích thước lớn, chứa nhiều thành phần dư thừa như SDK, công cụ build, mã nguồn và các gói không cần thiết cho môi trường chạy thật. Điều này không chỉ làm tăng thời gian build, push, pull và khởi động container mà còn làm mở rộng bề mặt tấn công, ảnh hưởng trực tiếp đến mức độ an toàn của hệ thống.

Kỹ thuật Docker Multi-stage Build ra đời để giải quyết bài toán trên bằng cách tách biệt rõ giai đoạn build và giai đoạn runtime. Thay vì đưa toàn bộ công cụ biên dịch vào image cuối cùng, kỹ thuật này chỉ sao chép các tệp đã publish sang image runtime tối giản hơn. Nhờ đó, image nhỏ hơn, gọn hơn và an toàn hơn.

Trong phạm vi đồ án này, nhóm lựa chọn hệ thống **website bán hàng GymBro** làm đối tượng nghiên cứu. Đây là một hệ thống thương mại điện tử xây dựng trên nền tảng .NET, có nhiều thành phần nghiệp vụ như quản lý sản phẩm, tài khoản người dùng, giỏ hàng, đơn hàng, thanh toán, tồn kho, đánh giá và khu vực quản trị. Việc ứng dụng Docker Multi-stage Build cho GymBro không chỉ mang ý nghĩa thực hành kỹ thuật mà còn tạo ra một tình huống nghiên cứu gần với thực tế triển khai doanh nghiệp.

Vì vậy, nhóm chọn đề tài này nhằm nghiên cứu cách áp dụng Docker Multi-stage Build để:

- Tối ưu kích thước Docker image.
- Tách biệt môi trường build và runtime.
- Giảm thành phần dư thừa trong image cuối.
- Tăng cường bảo mật container thông qua giảm attack surface và chạy non-root.
- Xây dựng cơ sở thực nghiệm để so sánh giữa mô hình baseline single-stage và mô hình multi-stage tối ưu.

### 1.2. Mục tiêu và nhiệm vụ nghiên cứu

#### Mục tiêu nghiên cứu

Mục tiêu tổng quát của đề tài là nghiên cứu, xây dựng và đánh giá mô hình Docker Multi-stage Build cho website bán hàng GymBro nhằm tối ưu hóa quá trình đóng gói ứng dụng và nâng cao mức độ an toàn khi triển khai container.

#### Nhiệm vụ nghiên cứu

Để đạt được mục tiêu trên, đề tài thực hiện các nhiệm vụ sau:

- Tìm hiểu cơ sở lý thuyết về Docker, containerization, Docker image và quá trình build image.
- Nghiên cứu nguyên lý hoạt động và lợi ích của kỹ thuật Multi-stage Build.
- Khảo sát hiện trạng triển khai Docker của hệ thống GymBro.
- Xây dựng mô hình đối chứng sử dụng Dockerfile single-stage để so sánh.
- Xây dựng mô hình Secure Multi-stage Build kết hợp hardening cho cả `GymBro.Web`, `GymBro.API` và cấu hình triển khai bằng Docker Compose.
- Thực hiện build, phân tích và so sánh hai mô hình theo các tiêu chí như kích thước image, số layer, số gói phần mềm, mức độ hiện diện của lỗ hổng và quyền thực thi của container.
- Đề xuất hướng cải tiến tiếp theo cho quy trình container hóa của GymBro.

### 1.3. Đối tượng và phạm vi nghiên cứu

#### Đối tượng nghiên cứu

Đối tượng nghiên cứu của đề tài là kỹ thuật xây dựng Docker image cho ứng dụng web ASP.NET Core/.NET, cụ thể là cách tổ chức Dockerfile theo mô hình single-stage và multi-stage.

#### Phạm vi nghiên cứu

Phạm vi thực nghiệm của đề tài tập trung vào hệ thống **website bán hàng GymBro** trong repository hiện tại, gồm các thành phần chính:

- `GymBro.Web`: giao diện website dành cho người dùng và quản trị.
- `GymBro.API`: dịch vụ API phục vụ các chức năng xác thực, sản phẩm và giỏ hàng.
- `GymBro.Infrastructure`, `GymBro.Application`, `GymBro.Core`: các project hỗ trợ nghiệp vụ và truy cập dữ liệu.
- `SQL Server`: cơ sở dữ liệu được khai báo trong `docker-compose.yml`.

Đề tài tập trung vào bài toán **đóng gói và triển khai container**, không đi sâu vào các nội dung ngoài phạm vi như Kubernetes, service mesh, bảo mật mạng nâng cao hoặc tối ưu ở mức hạ tầng cloud.

Trong phần triển khai, đề tài áp dụng mô hình Secure Multi-stage Build cho **toàn bộ hệ thống chạy bằng Docker**, gồm `GymBro.Web`, `GymBro.API`, `SQL Server` và cấu hình điều phối trong `docker-compose.yml`. Trong phần đo đạc định lượng, nhóm sử dụng `GymBro.Web` làm mẫu so sánh chi tiết giữa image single-stage và secure multi-stage vì đây là thành phần giao diện chính, có đầy đủ đặc điểm của ứng dụng ASP.NET Core runtime. Với `GymBro.API`, nhóm áp dụng cùng mô hình Dockerfile bảo mật, kiểm tra khả năng build, chạy non-root và hoạt động qua endpoint API.

### 1.4. Phương pháp nghiên cứu

Đề tài sử dụng kết hợp các phương pháp nghiên cứu sau:

- **Phương pháp nghiên cứu tài liệu:** tìm hiểu lý thuyết về Docker, Dockerfile, image layers, Multi-stage Build và hardening container.
- **Phương pháp phân tích hệ thống:** khảo sát cấu trúc solution GymBro, Dockerfile hiện có, docker-compose và luồng triển khai.
- **Phương pháp thực nghiệm:** xây dựng hai mô hình image, thực hiện build và ghi nhận các chỉ số kỹ thuật.
- **Phương pháp so sánh đối chứng:** so sánh mô hình single-stage với multi-stage trên cùng một mã nguồn và cùng môi trường build.
- **Phương pháp đánh giá định tính và định lượng:** đánh giá bằng số liệu thực tế như kích thước image, số packages, lỗ hổng và mức độ tối giản của image runtime.

## CHƯƠNG 2. CƠ SỞ LÝ THUYẾT

### 2.1. Tổng quan về Docker và Containerization

Docker là nền tảng cho phép đóng gói, phân phối và chạy ứng dụng trong các container. Container là một môi trường thực thi cô lập ở mức tiến trình, chia sẻ kernel của hệ điều hành host nhưng vẫn đảm bảo tính độc lập về filesystem, network và cấu hình chạy.

So với cách triển khai truyền thống trực tiếp trên máy chủ, containerization có nhiều ưu điểm:

- Môi trường chạy đồng nhất giữa máy lập trình, máy kiểm thử và máy chủ triển khai.
- Rút ngắn thời gian cài đặt phụ thuộc.
- Dễ sao chép, mở rộng và rollback.
- Hỗ trợ tốt cho CI/CD và phát triển theo kiến trúc dịch vụ.

Trong Docker, hai khái niệm quan trọng là:

- **Image:** khuôn mẫu bất biến dùng để tạo container.
- **Container:** thực thể đang chạy được sinh ra từ image.

Image thường được tạo thông qua `Dockerfile`, trong đó mỗi chỉ thị như `FROM`, `COPY`, `RUN`, `ENV` hay `ENTRYPOINT` sẽ đóng góp vào quá trình xây dựng image.

### 2.2. Kiến trúc Docker Image và Build Process

Một Docker image được xây dựng theo cấu trúc nhiều lớp (layers). Mỗi lệnh trong Dockerfile có thể tạo ra một layer mới. Docker tận dụng đặc điểm này để cache trong quá trình build, từ đó tăng tốc các lần build sau nếu các layer trước đó không thay đổi.

Quá trình build image thường diễn ra theo các bước:

1. Xác định base image bằng lệnh `FROM`.
2. Sao chép mã nguồn hoặc tệp cấu hình vào build context bằng `COPY`.
3. Cài đặt phụ thuộc hoặc biên dịch bằng `RUN`.
4. Thiết lập môi trường chạy qua `ENV`, `EXPOSE`, `WORKDIR`.
5. Xác định lệnh khởi động container bằng `CMD` hoặc `ENTRYPOINT`.

Về mặt kỹ thuật, chất lượng của Docker image phụ thuộc lớn vào:

- Việc chọn base image phù hợp.
- Cách tổ chức Dockerfile để tối ưu cache.
- Việc loại bỏ tệp thừa bằng `.dockerignore`.
- Mức độ tối giản của image runtime.
- Cách kiểm soát quyền thực thi và cấu hình bí mật.

Nếu build không hợp lý, image có thể trở nên quá lớn, khó bảo trì và tiềm ẩn nhiều rủi ro bảo mật.

### 2.3. Kỹ thuật Multi-stage Build

Multi-stage Build là kỹ thuật sử dụng nhiều lệnh `FROM` trong cùng một Dockerfile để tách các giai đoạn xử lý khác nhau của ứng dụng. Mỗi giai đoạn có thể có một vai trò riêng, chẳng hạn:

- `restore`: khôi phục packages.
- `build`: biên dịch ứng dụng.
- `test`: chạy kiểm thử.
- `publish`: đóng gói artifact cuối cùng.
- `final`: tạo image runtime.

Điểm cốt lõi của Multi-stage Build là image cuối chỉ nhận những gì thực sự cần để chạy ứng dụng, thông qua cú pháp `COPY --from=<stage>`.

So với cách build một giai đoạn, Multi-stage Build có các lợi ích nổi bật:

- Giảm kích thước image cuối.
- Loại bỏ SDK, compiler và mã nguồn khỏi runtime image.
- Giảm số gói phần mềm tồn tại trong image.
- Tăng tính rõ ràng của quy trình build.
- Nâng cao bảo mật do giảm attack surface.

Đối với các ứng dụng .NET, kỹ thuật này đặc biệt phù hợp vì giai đoạn build cần `mcr.microsoft.com/dotnet/sdk`, trong khi giai đoạn chạy thực tế chỉ cần `mcr.microsoft.com/dotnet/aspnet`.

### 2.4. Tổng quan về tối ưu hóa Docker Image cho ứng dụng ASP.NET Core

Khi triển khai ứng dụng ASP.NET Core bằng Docker, một số nguyên tắc tối ưu phổ biến gồm:

- Sử dụng đúng base image cho từng mục đích: `sdk` cho build, `aspnet` cho runtime.
- Tách riêng bước `restore` để tận dụng cache tốt hơn.
- Chỉ copy file publish vào image cuối.
- Loại bỏ tệp rác bằng `.dockerignore`.
- Chạy ứng dụng dưới non-root user nếu có thể.
- Không nhúng secrets trực tiếp vào Dockerfile.
- Tách cấu hình development và production.

Đối với đề tài này, tối ưu hóa không chỉ dừng ở mục tiêu giảm kích thước image mà còn phải hướng đến yếu tố **bảo mật**. Một image nhỏ hơn thường có:

- Ít packages hơn.
- Ít thành phần dư thừa hơn.
- Ít khả năng chứa lỗ hổng hơn.
- Ít công cụ bị khai thác hơn khi container bị xâm nhập.

Tuy vậy, image nhỏ hơn không đồng nghĩa tuyệt đối với an toàn hơn. Vì vậy, khi tối ưu image cho ASP.NET Core, cần kết hợp cả:

- Multi-stage Build.
- Quản lý secrets tốt hơn.
- Chạy non-root.
- Giảm cấu hình thừa ở runtime.
- Quét lỗ hổng image sau build.

### 2.5. Tổng quan ứng dụng Website bán hàng GymBro

GymBro là website bán hàng hướng đến lĩnh vực dụng cụ, phụ kiện hoặc sản phẩm liên quan đến thể hình và gym. Dựa trên cấu trúc mã nguồn hiện tại, hệ thống được tổ chức thành nhiều project theo chức năng:

- `GymBro.Web`: website MVC/Razor phục vụ giao diện người dùng.
- `GymBro.API`: API hỗ trợ xác thực, sản phẩm và giỏ hàng.
- `GymBro.Application`: xử lý use case và service ở tầng ứng dụng.
- `GymBro.Infrastructure`: truy cập cơ sở dữ liệu, xác thực, thanh toán, lưu trữ file.
- `GymBro.Core`: entity và mô hình miền dữ liệu.
- `GymBro.Tests`: kiểm thử.

Về mặt chức năng, GymBro hiện có các nhóm nghiệp vụ chính:

- Đăng ký, đăng nhập và quản lý tài khoản.
- Duyệt sản phẩm, danh mục và chi tiết sản phẩm.
- Thêm vào giỏ hàng và thanh toán.
- Quản lý đơn hàng và lịch sử mua hàng.
- Quản lý phương thức thanh toán.
- Quản lý tồn kho, nhà cung cấp và phiếu nhập.
- Đánh giá sản phẩm và wishlist.
- Khu vực quản trị cho sản phẩm, người dùng, đơn hàng và thanh toán.

Hệ thống triển khai bằng Docker Compose với các dịch vụ chính:

- `db`: SQL Server.
- `gymbro_api`: API của hệ thống.
- `gymbro_web`: website giao diện chính.
- `gymbro_migrator`: service chạy EF migration trước khi Web/API khởi động.

Điều này tạo điều kiện phù hợp để nghiên cứu việc container hóa trong một mô hình gần với hệ thống thương mại điện tử thực tế.

## CHƯƠNG 3. ĐỀ XUẤT MÔ HÌNH

### 3.1. Kiến trúc triển khai tổng thể

Kiến trúc triển khai tổng thể của hệ thống GymBro trong môi trường Docker có thể mô tả như sau:

```text
Người dùng
   |
   v
GymBro.Web (ASP.NET Core MVC)
   |
   +------> GymBro.API (ASP.NET Core Web API)
   |             |
   |             v
   +---------> SQL Server
```

Trong đó:

- `GymBro.Web` là giao diện chính mà người dùng truy cập qua trình duyệt.
- `GymBro.API` cung cấp các endpoint hỗ trợ xác thực và một số chức năng nghiệp vụ.
- `SQL Server` lưu trữ dữ liệu hệ thống.
- `gymbro_migrator` chạy migration bundle để cập nhật database trước khi Web/API khởi động.
- `docker-compose.yml` chịu trách nhiệm điều phối network, ports, environment variables và thứ tự khởi động container trong môi trường build/demo.
- `docker-compose.runtime.yml` dùng cho kịch bản chạy bằng image đã build sẵn, không cần mang source code sang máy triển khai.

Trước khi cải tiến, hệ thống đã có Dockerfile cho `GymBro.Web` và `GymBro.API`, đồng thời sử dụng Multi-stage Build cơ bản. Tuy nhiên vẫn còn một số điểm có thể cải thiện:

- `docker-compose.yml` đang để `ASPNETCORE_ENVIRONMENT=Development`.
- Mật khẩu `sa` và connection string đang được khai báo trực tiếp trong file compose.
- `GymBro.API` chưa có bước hardening non-root rõ ràng.
- Bước `restore` trong Dockerfile web chưa copy đủ `GymBro.Application.csproj`, làm giảm hiệu quả cache.

Từ hiện trạng đó, đề tài đề xuất xây dựng mô hình so sánh gồm:

- **Mô hình đối chứng:** Dockerfile single-stage dùng để so sánh định lượng.
- **Mô hình tối ưu:** Secure Multi-stage Build áp dụng cho cả `GymBro.Web`, `GymBro.API`, migration bundle và cấu hình hardening trong `docker-compose.yml`/`docker-compose.runtime.yml`.

### 3.2. Mô hình Dockerfile (Baseline - Single-stage)

Để có cơ sở đối chứng, nhóm xây dựng Dockerfile single-stage cho thành phần ứng dụng .NET. Trong báo cáo, `GymBro.Web` được dùng làm mẫu đo định lượng vì đây là thành phần giao diện chính của website bán hàng GymBro; nếu áp dụng cùng cách viết single-stage cho `GymBro.API` thì bản chất rủi ro vẫn giống nhau: image runtime chứa SDK, source code, công cụ build và thường chạy bằng user mặc định.

Mô hình baseline minh họa cho `GymBro.Web` như sau:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0
WORKDIR /src

COPY . .
WORKDIR /src/GymBro.Web

RUN dotnet restore "GymBro.Web.csproj"
RUN dotnet publish "GymBro.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

WORKDIR /app/publish
ENTRYPOINT ["dotnet", "GymBro.Web.dll"]
```

Đặc điểm của mô hình này:

- Toàn bộ quá trình restore, build và runtime đều diễn ra trong cùng một image.
- Image cuối vẫn chứa SDK .NET.
- Mã nguồn và nhiều công cụ build tồn tại trong image runtime.
- Container chạy với user mặc định là `root`.

Ưu điểm của mô hình single-stage là dễ viết và dễ hiểu đối với người mới làm quen với Docker. Tuy nhiên, nhược điểm rất rõ:

- Image lớn.
- Chứa nhiều packages không cần thiết.
- Tăng nguy cơ tồn tại lỗ hổng.
- Không tách biệt build-time và runtime.
- Không phù hợp cho môi trường production.

### 3.3. Mô hình Dockerfile (Secure Multi-stage Build - Tối ưu theo hướng bảo mật)

Mô hình được đề xuất cho GymBro không chỉ là Multi-stage Build theo nghĩa thông thường. Nếu chỉ áp dụng Multi-stage Build bình thường, mục tiêu chính thường là tách giai đoạn build và runtime để giảm kích thước image. Trong đề tài này, nhóm mở rộng mô hình đó thành **Secure Multi-stage Build**, tức là dùng Multi-stage Build làm nền tảng để tối ưu image và bổ sung các yếu tố hardening nhằm tăng cường bảo mật.

Vì vậy, điểm khác biệt của mô hình đề xuất là:

- Không chỉ giảm kích thước image, mà còn giảm bề mặt tấn công.
- Không chỉ tách SDK khỏi runtime, mà còn chạy container bằng non-root user.
- Không chỉ tạo image chạy được, mà còn chuẩn bị stage kiểm thử và mô hình triển khai an toàn hơn.
- Không chỉ tối ưu Dockerfile, mà còn kết hợp với `docker-compose.yml` và `docker-compose.runtime.yml` để xử lý secrets, quyền ghi và runtime hardening.

Mẫu Dockerfile Secure Multi-stage Build cho `GymBro.Web` có thể tổ chức như sau:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS restore
WORKDIR /src

COPY ["GymBro.Web/GymBro.Web.csproj", "GymBro.Web/"]
COPY ["GymBro.Application/GymBro.Application.csproj", "GymBro.Application/"]
COPY ["GymBro.Core/GymBro.Core.csproj", "GymBro.Core/"]
COPY ["GymBro.Infrastructure/GymBro.Infrastructure.csproj", "GymBro.Infrastructure/"]
COPY ["GymBro.Tests/GymBro.Tests.csproj", "GymBro.Tests/"]

RUN dotnet restore "GymBro.Web/GymBro.Web.csproj"
RUN dotnet restore "GymBro.Tests/GymBro.Tests.csproj"

FROM restore AS build
COPY . .
WORKDIR /src/GymBro.Web
RUN dotnet build "GymBro.Web.csproj" -c Release --no-restore -o /app/build

FROM build AS test
WORKDIR /src
RUN dotnet test "GymBro.Tests/GymBro.Tests.csproj" -c Release --no-restore

FROM test AS migration-bundle
WORKDIR /src
RUN dotnet tool install --tool-path /opt/dotnet-tools dotnet-ef --version 8.0.0
RUN /opt/dotnet-tools/dotnet-ef migrations bundle \
    --project "GymBro.Infrastructure/GymBro.Infrastructure.csproj" \
    --startup-project "GymBro.Web/GymBro.Web.csproj" \
    --configuration Release \
    --output /app/migrator/gymbro-migrate \
    --force

FROM test AS publish
WORKDIR /src/GymBro.Web
RUN dotnet publish "GymBro.Web.csproj" -c Release --no-restore -o /app/publish /p:UseAppHost=false /p:DebugType=None /p:DebugSymbols=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=publish /app/publish .

RUN rm -f ./*.pdb ./appsettings.Development.json

RUN adduser --disabled-password --gecos "" gymuser \
    && mkdir -p /app/wwwroot/Content/Images /home/gymuser/.aspnet/DataProtection-Keys \
    && chown -R gymuser:gymuser /app /home/gymuser

USER gymuser

ENTRYPOINT ["dotnet", "GymBro.Web.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS migrator
WORKDIR /app
ENV DOTNET_BUNDLE_EXTRACT_BASE_DIR=/tmp/dotnet-bundle

COPY --from=migration-bundle /app/migrator/gymbro-migrate .

RUN adduser --disabled-password --gecos "" gymmigrator \
    && chmod +x /app/gymbro-migrate \
    && chown -R gymmigrator:gymmigrator /app

USER gymmigrator

ENTRYPOINT ["/app/gymbro-migrate"]
```

Đối với `GymBro.API`, nhóm áp dụng cùng mô hình. Điểm khác là Dockerfile restore, build và publish project API, đồng thời dùng user runtime riêng tên `gymapi`:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS restore
WORKDIR /src

COPY ["GymBro.API/GymBro.API.csproj", "GymBro.API/"]
COPY ["GymBro.Application/GymBro.Application.csproj", "GymBro.Application/"]
COPY ["GymBro.Core/GymBro.Core.csproj", "GymBro.Core/"]
COPY ["GymBro.Infrastructure/GymBro.Infrastructure.csproj", "GymBro.Infrastructure/"]
COPY ["GymBro.Tests/GymBro.Tests.csproj", "GymBro.Tests/"]

RUN dotnet restore "GymBro.API/GymBro.API.csproj"
RUN dotnet restore "GymBro.Tests/GymBro.Tests.csproj"

FROM restore AS build
COPY . .
WORKDIR /src/GymBro.API
RUN dotnet build "GymBro.API.csproj" -c Release --no-restore -o /app/build

FROM build AS test
WORKDIR /src
RUN dotnet test "GymBro.Tests/GymBro.Tests.csproj" -c Release --no-restore

FROM test AS publish
WORKDIR /src/GymBro.API
RUN dotnet publish "GymBro.API.csproj" -c Release --no-restore -o /app/publish /p:UseAppHost=false /p:DebugType=None /p:DebugSymbols=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=publish /app/publish .

RUN rm -f ./*.pdb ./appsettings.Development.json

RUN adduser --disabled-password --gecos "" gymapi \
    && mkdir -p /home/gymapi/.aspnet/DataProtection-Keys \
    && chown -R gymapi:gymapi /app /home/gymapi

USER gymapi

ENTRYPOINT ["dotnet", "GymBro.API.dll"]
```

So với baseline single-stage, mô hình này có các cải tiến chính:

- Không mang SDK vào image runtime.
- Chỉ sao chép artifact đã publish vào image cuối.
- Giảm packages và layers không cần thiết.
- Chạy container dưới tài khoản `gymuser` thay vì `root`.
- Tăng hiệu quả cache build nếu tách bước `restore` đúng cách.
- Có stage `test` để CI/CD có thể kiểm thử trước khi xuất bản image.
- Chuẩn bị sẵn các thư mục cần quyền ghi để khi kết hợp với volume trong Docker Compose có thể chạy với filesystem `read_only`.
- Không tạo debug symbols trong output publish và xóa `.pdb`, `appsettings.Development.json` khỏi final image.
- Tách `gymbro_migrator` thành migration bundle chạy trong runtime image riêng, thay vì chạy trực tiếp bằng SDK image có chứa source code.

Như vậy, Secure Multi-stage Build không chỉ được áp dụng cho riêng `GymBro.Web`. Cả hai entry point chính của hệ thống là `GymBro.Web` và `GymBro.API` đều được đóng gói theo cùng nguyên tắc: build bằng SDK ở stage trung gian, chạy bằng ASP.NET runtime ở stage cuối, không mang SDK/source code sang runtime image và dùng non-root user. Ngoài ra, service `gymbro_migrator` cũng được tách thành image runtime chứa file migration bundle `gymbro-migrate`, giúp chạy cập nhật database mà không phải mang toàn bộ mã nguồn hoặc công cụ `dotnet-ef` vào môi trường triển khai.

Bảng sau thể hiện sự khác nhau giữa Multi-stage Build thông thường và Secure Multi-stage Build mà đề tài đề xuất:

| Tiêu chí | Multi-stage Build thông thường | Secure Multi-stage Build đề xuất |
| --- | --- | --- |
| Mục tiêu chính | Giảm kích thước image và tách build/runtime | Giảm kích thước image, giảm attack surface và tăng hardening |
| Image runtime | Dùng runtime image, chỉ copy artifact cần thiết | Dùng runtime image, chỉ copy artifact cần thiết và loại bỏ tối đa thành phần dư thừa |
| Quyền chạy container | Có thể vẫn chạy user mặc định/root | Chạy bằng non-root user như `gymuser` hoặc `gymapi` |
| Kiểm thử | Thường không thể hiện rõ trong Dockerfile | Có stage `test` để phục vụ kiểm thử trong pipeline |
| Secrets | Thường không xử lý trực tiếp | Kết hợp Docker Compose để không hard-code mật khẩu database và JWT key trong image/file compose |
| Quyền ghi filesystem | Thường không kiểm soát rõ | Kết hợp volume, `read_only`, `tmpfs` cho các vị trí cần ghi |
| Runtime hardening | Ít hoặc không đề cập | Kết hợp `no-new-privileges`, `cap_drop`, non-root và image tối giản |
| Tiêu chí đánh giá | Chủ yếu là image size và build time | Image size, số packages, lỗ hổng, user runtime và mức độ cấu hình an toàn |

Như vậy, phần 3.3 không còn là mô hình "áp dụng Multi-stage Build bình thường" mà là mô hình **Multi-stage Build theo hướng bảo mật**. Đây cũng là điểm giúp đề tài khác với một bài thực hành Dockerfile đơn thuần: nhóm không chỉ chứng minh image nhỏ hơn, mà còn chứng minh image runtime ít thành phần hơn, ít rủi ro hơn và được triển khai theo nguyên tắc least privilege.

#### Phân tích ý nghĩa các nhóm lệnh trong Dockerfile

Để người đọc dễ hình dung hơn, có thể hiểu Dockerfile trên như một dây chuyền gồm nhiều khu vực làm việc. Các khu vực đầu dùng để tải thư viện, biên dịch và kiểm thử. Khu vực cuối cùng mới là phần được đóng gói thành image chạy thật.

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS restore
WORKDIR /src
```

Hai dòng này tạo stage đầu tiên tên là `restore`, dùng image `dotnet/sdk:9.0`. Đây là image nặng vì có đầy đủ SDK, compiler và công cụ build. Việc đặt tên stage là `restore` giúp phân biệt rõ đây chỉ là môi trường tạm dùng để khôi phục thư viện, không phải môi trường chạy cuối cùng. Về bảo mật, điểm quan trọng là SDK chỉ tồn tại ở stage trung gian; nếu về sau không copy toàn bộ stage này sang image cuối thì compiler và công cụ build sẽ không xuất hiện trong runtime image.

```dockerfile
COPY ["GymBro.Web/GymBro.Web.csproj", "GymBro.Web/"]
COPY ["GymBro.Application/GymBro.Application.csproj", "GymBro.Application/"]
COPY ["GymBro.Core/GymBro.Core.csproj", "GymBro.Core/"]
COPY ["GymBro.Infrastructure/GymBro.Infrastructure.csproj", "GymBro.Infrastructure/"]
COPY ["GymBro.Tests/GymBro.Tests.csproj", "GymBro.Tests/"]
```

Nhóm lệnh này chỉ sao chép các file `.csproj` trước, chưa sao chép toàn bộ mã nguồn. Cách làm này giúp Docker cache bước restore tốt hơn: nếu mã nguồn `.cs` thay đổi nhưng file `.csproj` không đổi, Docker có thể tái sử dụng layer restore cũ. Về bảo mật, cách này không trực tiếp chặn tấn công, nhưng giúp quy trình build rõ ràng, có kiểm soát và giảm việc copy dư thừa quá sớm.

```dockerfile
RUN dotnet restore "GymBro.Web/GymBro.Web.csproj"
RUN dotnet restore "GymBro.Tests/GymBro.Tests.csproj"
```

Hai dòng này tải các gói NuGet cần thiết cho ứng dụng và test. Chúng được đặt trong stage `restore`, tức là các cache và công cụ restore nằm ở môi trường build tạm thời. Kết quả cuối cùng không đưa toàn bộ cache NuGet hay SDK vào image runtime, nhờ đó image cuối gọn hơn và ít bề mặt tấn công hơn.

```dockerfile
FROM restore AS build
COPY . .
WORKDIR /src/GymBro.Web
RUN dotnet build "GymBro.Web.csproj" -c Release --no-restore -o /app/build
```

Stage `build` kế thừa từ `restore`, sau đó mới copy toàn bộ mã nguồn để biên dịch. Tùy chọn `--no-restore` cho biết bước restore đã được thực hiện trước đó, tránh lặp lại thao tác tải thư viện. Chế độ `Release` tạo bản build phù hợp hơn cho triển khai. Stage này vẫn là stage tạm, vì nó có mã nguồn và công cụ build; sau khi build xong, stage này không được dùng trực tiếp làm image chạy thật.

```dockerfile
FROM build AS test
WORKDIR /src
RUN dotnet test "GymBro.Tests/GymBro.Tests.csproj" -c Release --no-restore
```

Stage `test` dùng để chạy kiểm thử trước khi publish. Đây là điểm khác với Multi-stage Build tối giản thông thường, vì nó biến Dockerfile thành một phần của pipeline kiểm soát chất lượng. Nếu test thất bại, quá trình build image sẽ dừng lại, tránh tạo ra image chứa phiên bản ứng dụng có lỗi. Về bảo mật và vận hành, điều này giúp giảm rủi ro đưa một artifact chưa được kiểm tra vào môi trường triển khai.

```dockerfile
FROM test AS migration-bundle
WORKDIR /src
RUN dotnet tool install --tool-path /opt/dotnet-tools dotnet-ef --version 8.0.0
RUN /opt/dotnet-tools/dotnet-ef migrations bundle \
    --project "GymBro.Infrastructure/GymBro.Infrastructure.csproj" \
    --startup-project "GymBro.Web/GymBro.Web.csproj" \
    --configuration Release \
    --output /app/migrator/gymbro-migrate \
    --force
```

Stage `migration-bundle` tạo file thực thi `gymbro-migrate` dùng để cập nhật database. Điểm bảo mật quan trọng là `dotnet-ef`, SDK và toàn bộ source code chỉ tồn tại trong stage trung gian. Image migrator cuối cùng chỉ nhận file bundle đã đóng gói, nhờ đó máy chạy demo hoặc máy triển khai không cần chứa mã nguồn để thực hiện migration.

```dockerfile
FROM test AS publish
WORKDIR /src/GymBro.Web
RUN dotnet publish "GymBro.Web.csproj" -c Release --no-restore -o /app/publish /p:UseAppHost=false /p:DebugType=None /p:DebugSymbols=false
```

Stage `publish` tạo ra bộ file tối thiểu cần để chạy ứng dụng trong thư mục `/app/publish`. Đây là điểm mấu chốt của Multi-stage Build: thay vì đưa cả source code, SDK, cache và file trung gian vào runtime image, Dockerfile chỉ lấy thư mục đã publish. Tùy chọn `/p:UseAppHost=false` giúp không tạo executable app host riêng, còn `/p:DebugType=None /p:DebugSymbols=false` giúp không đưa debug symbols vào output publish.

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
```

Đây là stage cuối cùng, cũng là stage tạo ra image thật sau khi chạy `docker build`. Image nền được đổi từ `dotnet/sdk` sang `dotnet/aspnet`. Image `aspnet` chỉ có runtime cần thiết để chạy ứng dụng ASP.NET Core, không có SDK để biên dịch mã nguồn. Vì vậy, nếu container bị khai thác, kẻ tấn công cũng có ít công cụ sẵn có hơn so với image SDK. Đây là một trong các tác dụng bảo mật quan trọng nhất của Multi-stage Build.

```dockerfile
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
```

Hai dòng này cấu hình ứng dụng lắng nghe HTTP trên cổng `8080` bên trong container. `EXPOSE 8080` không tự mở port ra ngoài máy host, mà chỉ mô tả port ứng dụng dùng trong container. Việc publish port thật được kiểm soát ở `docker-compose.yml`, ví dụ chỉ bind ra `127.0.0.1:5000`. Cách tách này giúp kiểm soát bề mặt mạng tốt hơn.

```dockerfile
COPY --from=publish /app/publish .
RUN rm -f ./*.pdb ./appsettings.Development.json
```

Đây là nhóm lệnh quan trọng nhất để hình dung kết quả cuối cùng. Nó chỉ copy thư mục `/app/publish` từ stage `publish` sang stage `final`. Những thứ ở stage trước như source code đầy đủ, SDK, compiler, NuGet cache, thư mục build trung gian và test project sẽ không tự động đi vào image cuối. Sau đó, Dockerfile xóa thêm các file `.pdb` và `appsettings.Development.json` để giảm thông tin debug/dev không cần thiết trong runtime image. Nhờ đó, runtime image cuối nhỏ hơn, sạch hơn và khó bị khai thác hơn.

```dockerfile
RUN adduser --disabled-password --gecos "" gymuser \
    && mkdir -p /app/wwwroot/Content/Images /home/gymuser/.aspnet/DataProtection-Keys \
    && chown -R gymuser:gymuser /app /home/gymuser
```

Nhóm lệnh này tạo user `gymuser` không có mật khẩu đăng nhập, tạo các thư mục mà ứng dụng cần ghi dữ liệu như thư mục ảnh sản phẩm và key Data Protection, sau đó chuyển quyền sở hữu cho `gymuser`. Ý nghĩa bảo mật là container không cần chạy bằng tài khoản `root`. Nếu ứng dụng bị khai thác, tiến trình bị chiếm quyền chỉ có quyền của `gymuser`, không có toàn quyền quản trị trong container.

```dockerfile
USER gymuser
```

Dòng này buộc ứng dụng chạy bằng user `gymuser` thay vì user mặc định `root`. Đây là nguyên tắc **least privilege**: tiến trình chỉ có quyền tối thiểu cần thiết để chạy. Trong demo của GymBro, khi kiểm tra image sau build, image `GymBro.Web` cho thấy user runtime là `gymuser`, còn `GymBro.API` là `gymapi`.

```dockerfile
ENTRYPOINT ["dotnet", "GymBro.Web.dll"]
```

Dòng cuối cùng khai báo lệnh khởi động container. Khi container chạy, Docker sẽ gọi `dotnet GymBro.Web.dll` trong thư mục `/app` bằng user `gymuser`. Lúc này container không còn hoạt động như một máy build nữa, mà chỉ là môi trường runtime chạy file đã publish.

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS migrator
WORKDIR /app
ENV DOTNET_BUNDLE_EXTRACT_BASE_DIR=/tmp/dotnet-bundle
COPY --from=migration-bundle /app/migrator/gymbro-migrate .
USER gymmigrator
ENTRYPOINT ["/app/gymbro-migrate"]
```

Stage `migrator` là image runtime riêng cho migration. Biến `DOTNET_BUNDLE_EXTRACT_BASE_DIR=/tmp/dotnet-bundle` giúp migration bundle giải nén file tạm vào `/tmp`; trong Docker Compose, `/tmp` được mount bằng `tmpfs`, nên container vẫn giữ được root filesystem ở chế độ `read_only`. Kết quả là service migration có thể chạy được nhưng vẫn không cần source code, SDK hoặc quyền ghi rộng trên filesystem.

Kết quả cuối cùng sau khi chạy Dockerfile:

- Image cuối được tạo từ stage `final`, không phải từ stage `restore`, `build`, `test` hay `publish`.
- Image cuối dùng base image `mcr.microsoft.com/dotnet/aspnet:9.0`, không dùng `mcr.microsoft.com/dotnet/sdk:9.0`.
- Trong image cuối chỉ có các file publish cần để chạy `GymBro.Web.dll`.
- SDK, compiler, source code build đầy đủ, cache restore, file trung gian, `.pdb` và cấu hình development không được copy hoặc bị loại khỏi runtime image.
- Ứng dụng chạy trên port nội bộ `8080`.
- Ứng dụng chạy bằng non-root user `gymuser`.
- Các thư mục cần ghi được tạo và phân quyền trước, để khi kết hợp với Docker Compose có thể dùng volume, `read_only` và `tmpfs` an toàn hơn.
- Với `GymBro.API`, kết quả cuối cùng là API chạy bằng user `gymapi` trong image runtime riêng, cũng không mang SDK vào môi trường chạy thật và nhận JWT key qua biến môi trường.
- Với `gymbro_migrator`, kết quả cuối cùng là image runtime chứa file `gymbro-migrate`, chạy bằng user `gymmigrator`, không còn chứa source code `.cs`, file `.csproj`, solution `.sln` hay project test.

#### Cấu hình Docker Compose theo hướng bảo mật

Ngoài Dockerfile, mô hình đề xuất còn harden ở mức Docker Compose. Các điểm chính gồm:

- `GYMBRO_DB_PASSWORD` và `GYMBRO_JWT_KEY` được truyền qua biến môi trường, không hard-code trực tiếp trong image hoặc compose chính.
- Web, API và migrator đều bật `read_only: true` để root filesystem không bị ghi tùy ý khi container đang chạy.
- `/tmp` được khai báo bằng `tmpfs`, cho phép ứng dụng ghi dữ liệu tạm mà không phá vỡ nguyên tắc root filesystem chỉ đọc.
- `cap_drop: ALL` giảm Linux capabilities mặc định của container.
- `security_opt: no-new-privileges:true` ngăn tiến trình trong container tự nâng quyền.
- Các port public được bind vào `127.0.0.1` để giảm phạm vi phơi bày trên mạng host trong môi trường demo.
- `docker-compose.runtime.yml` chỉ dùng `image:` và không dùng `build:`, phù hợp cho kịch bản máy khác chỉ nhận image đã build sẵn để chạy, không cần có mã nguồn dự án.

### 3.4. Quy trình build – test – deploy

Quy trình build, test và deploy được đề xuất cho GymBro gồm các bước sau:

1. **Restore dependencies**
   - Khôi phục các gói NuGet của solution.
2. **Build**
   - Biên dịch project ở chế độ Release.
3. **Test**
   - Chạy kiểm thử với project `GymBro.Tests`.
4. **Publish**
   - Xuất bản artifact để chạy trong môi trường runtime.
5. **Build migration bundle**
   - Tạo file `gymbro-migrate` để cập nhật database mà không cần mang source code sang môi trường chạy.
6. **Build Docker image**
   - Tạo image theo Dockerfile multi-stage, gồm Web, API và migrator.
7. **Scan image**
   - Dùng Docker Scout để kiểm tra packages và lỗ hổng.
8. **Deploy**
   - Trong môi trường build/demo, khởi chạy hệ thống bằng `docker-compose.yml`.
   - Trong môi trường chỉ chạy image, dùng `docker-compose.runtime.yml` để không cần source code.

Quy trình này có thể mô tả ngắn gọn như sau:

```text
Source Code
   -> Restore
   -> Build
   -> Test
   -> Publish
   -> Migration Bundle
   -> Docker Build
   -> Docker Scan
   -> Docker Compose Up
```

Nếu áp dụng vào CI/CD, quy trình trên sẽ giúp nhóm đảm bảo image trước khi deploy không chỉ chạy được mà còn đáp ứng mức tối ưu và an toàn tốt hơn.

## CHƯƠNG 4. THỰC NGHIỆM VÀ ĐÁNH GIÁ

### 4.1. Thiết lập môi trường

Phần thực nghiệm trong báo cáo được thực hiện trên toàn bộ hệ thống Docker của GymBro. Hệ thống sau khi cải tiến gồm `GymBro.Web`, `GymBro.API`, `SQL Server` và service `gymbro_migrator` chạy migration trước khi Web/API khởi động. Trong đó, `GymBro.Web` được dùng làm mẫu đo định lượng chi tiết giữa single-stage và secure multi-stage; `GymBro.API` được áp dụng cùng mô hình Dockerfile và được kiểm chứng bằng build image, non-root runtime và endpoint API. Riêng `gymbro_migrator` được kiểm chứng theo hướng migration bundle, tức là image runtime chỉ chứa file thực thi migration, không chứa source code hoặc SDK.

Môi trường thực nghiệm:

- Hệ điều hành host: Windows.
- Docker version: `28.5.1`.
- Docker Compose version: `v2.40.2-desktop.1`.
- Base image build: `mcr.microsoft.com/dotnet/sdk:9.0`.
- Base image runtime: `mcr.microsoft.com/dotnet/aspnet:9.0`.
- Công cụ quét image: `docker scout quickview`.

Các image/service được build hoặc khởi chạy trong thực nghiệm:

- `gymbro-web-single-report`: mô hình single-stage baseline.
- `gymbro/web:secure-multistage`: mô hình secure multi-stage cho `GymBro.Web`.
- `gymbro/api:secure-multistage`: mô hình secure multi-stage cho `GymBro.API`.
- `gymbro/migrator:secure-multistage`: service chạy EF migration bằng migration bundle để chuẩn bị database cho toàn hệ thống.
- `mcr.microsoft.com/mssql/server:2022-latest`: SQL Server dùng trong `docker-compose.yml`.

Các tiêu chí đánh giá gồm:

- Kích thước image.
- Số layer.
- User chạy container.
- Sự hiện diện của .NET SDK trong runtime image.
- Sự hiện diện của source code, file `.csproj`, file `.pdb` và cấu hình development trong final image.
- Cách xử lý secrets như mật khẩu database và JWT key.
- Số packages được Docker Scout index.
- Số lượng lỗ hổng theo mức `Critical`, `High`, `Medium`, `Low`.

### 4.2. Thực nghiệm Dockerfile Single-stage

Dockerfile baseline được build bằng mô hình một giai đoạn, trong đó cùng một image đảm nhiệm cả restore, publish và runtime.

Lệnh build thực nghiệm:

```powershell
@'
FROM mcr.microsoft.com/dotnet/sdk:9.0
WORKDIR /src
COPY . .
WORKDIR /src/GymBro.Web
RUN dotnet restore "GymBro.Web.csproj"
RUN dotnet publish "GymBro.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false
WORKDIR /app/publish
ENTRYPOINT ["dotnet", "GymBro.Web.dll"]
'@ | docker build -t gymbro-web-single-report -f - .
```

Kết quả quan sát được:

- Image được tạo thành công.
- Kích thước image theo `docker images`: **2.14GB**.
- Số layer theo `docker history`: **21 layer**.
- User mặc định khi chạy container: **UID 0 (root)**.
- Runtime image vẫn chứa .NET SDK.
- Kết quả `dotnet --list-sdks` trong container:

```text
9.0.312 [/usr/share/dotnet/sdk]
```

- Docker Scout index **1519 packages**.
- Kết quả quét nhanh:
  - `Critical`: **1**
  - `High`: **9**
  - `Medium`: **10**
  - `Low`: **47**

Nhận xét:

- Image rất lớn vì chứa cả môi trường build.
- Số packages cao dẫn đến số lỗ hổng tiềm ẩn cũng lớn hơn.
- Việc container chạy bằng `root` không phù hợp với định hướng hardening.
- Đây là mô hình dễ xây dựng nhưng không phù hợp cho production.

### 4.3. Thực nghiệm Dockerfile Secure Multi-stage Build

Tiếp theo, nhóm thực hiện build image từ Dockerfile Secure Multi-stage Build của `GymBro.Web`, `GymBro.API` và `gymbro_migrator`.

Lệnh build cho `GymBro.Web`:

```powershell
docker build --target final -t gymbro/web:secure-multistage -f GymBro.Web/Dockerfile .
```

Lệnh build cho `GymBro.API`:

```powershell
docker build --target final -t gymbro/api:secure-multistage -f GymBro.API/Dockerfile .
```

Lệnh build cho `gymbro_migrator`:

```powershell
docker build --target migrator -t gymbro/migrator:secure-multistage -f GymBro.Web/Dockerfile .
```

Kết quả quan sát được với `GymBro.Web`:

- Image được tạo thành công.
- Kích thước image theo `docker images`: **415MB**.
- Số layer theo `docker history`: **17 layer**.
- User chạy container: **`gymuser` (UID 1000)**.
- Runtime image **không còn .NET SDK**.
- Kết quả `dotnet --list-sdks` trong container không trả về SDK nào.
- Docker Scout index **178 packages**.
- Kết quả quét nhanh:
  - `Critical`: **0**
  - `High`: **5**
  - `Medium`: **6**
  - `Low`: **28**

Kết quả quan sát được với `GymBro.API`:

- Image được tạo thành công.
- Kích thước image theo `docker images`: **383MB**.
- Số layer theo `docker history`: **17 layer**.
- Dockerfile sử dụng cùng mô hình `restore -> build -> test -> publish -> final`.
- Image runtime dùng `mcr.microsoft.com/dotnet/aspnet:9.0`, không dùng SDK.
- Container chạy bằng user riêng `gymapi`, không chạy bằng `root`.
- JWT secret không được hard-code trong image; container nhận giá trị qua biến môi trường `GYMBRO_JWT_KEY`.
- Image API không còn file `.cs`, `.csproj`, `.sln`, `.pdb` hoặc `appsettings.Development.json`.
- Docker Scout index **186 packages**, kết quả quét nhanh gồm `0 Critical`, `6 High`, `6 Medium`, `28 Low`.
- Khi triển khai bằng Docker Compose, endpoint `http://127.0.0.1:5001/api/product` trả về `200`, cho thấy API hoạt động trong mô hình secure multi-stage.

Kết quả quan sát được với `gymbro_migrator`:

- Image được tạo thành công bằng target `migrator`.
- Kích thước image theo `docker images`: **423MB**.
- Số layer theo `docker history`: **15 layer**.
- Image runtime dùng `mcr.microsoft.com/dotnet/aspnet:9.0`, không dùng SDK.
- Image chỉ chứa file migration bundle `gymbro-migrate`.
- Container chạy bằng user riêng `gymmigrator`, không chạy bằng `root`.
- Khi kiểm tra image, không còn file `.cs`, `.csproj`, `.sln`, `.pdb`, `appsettings.Development.json` hoặc project test.
- Container có thể chạy với `read_only: true` nhờ dùng `DOTNET_BUNDLE_EXTRACT_BASE_DIR=/tmp/dotnet-bundle` và `tmpfs` cho `/tmp`.
- Docker Scout index **138 packages**, kết quả quét nhanh gồm `0 Critical`, `3 High`, `2 Medium`, `28 Low`.
- Khi triển khai bằng Docker Compose, `gymbro_migrator` kết thúc với exit code `0`.

Nhận xét:

- Image nhỏ hơn rõ rệt so với mô hình single-stage.
- Số packages giảm mạnh nhờ loại bỏ SDK và thành phần build.
- Việc sử dụng non-root user giúp tăng mức độ an toàn khi runtime cho Web, API và migrator.
- Việc chuyển migration sang bundle giúp toàn bộ luồng demo không cần mang source code sang image runtime.
- `docker-compose.runtime.yml` cho phép chạy hệ thống bằng các image đã build sẵn trên máy khác chỉ có Docker, miễn là có đủ image và biến môi trường cần thiết.
- Mô hình này phù hợp hơn cho triển khai thực tế vì không chỉ tối ưu Dockerfile mà còn áp dụng cho toàn bộ luồng chạy của hệ thống GymBro.

### 4.4. Phân tích và đánh giá kết quả

Bảng sau là kết quả định lượng chi tiết trên `GymBro.Web`, thành phần được chọn làm mẫu đo image size, số packages và số lỗ hổng. `GymBro.API` cũng được áp dụng cùng mô hình Secure Multi-stage Build và được kiểm chứng ở mức build thành công, chạy non-root, không dùng SDK trong runtime và endpoint API hoạt động. `gymbro_migrator` được kiểm chứng bằng migration bundle, non-root runtime, không chứa source code và exit code `0`.

| Tiêu chí | Single-stage | Multi-stage | Đánh giá |
| --- | --- | --- | --- |
| Kích thước image | 2.14GB | 415MB | Giảm khoảng 80.6% |
| Số layer | 21 | 17 | Giảm 4 layer |
| User runtime | root (UID 0) | gymuser (UID 1000) | Multi-stage an toàn hơn |
| SDK trong image runtime | Có | Không | Multi-stage tối giản hơn |
| Packages được index | 1519 | 178 | Giảm khoảng 88.3% |
| Critical vulnerabilities | 1 | 0 | Giảm 100% |
| High vulnerabilities | 9 | 5 | Giảm khoảng 44.4% |
| Medium vulnerabilities | 10 | 6 | Giảm 40% |
| Low vulnerabilities | 47 | 28 | Giảm khoảng 40.4% |

Từ bảng trên có thể rút ra các nhận định sau:

#### Về hiệu quả tối ưu hóa

Với thành phần `GymBro.Web`, Multi-stage Build giúp giảm kích thước image rất đáng kể. Đây là lợi ích trực tiếp nhất và dễ quan sát nhất. Khi áp dụng cùng nguyên tắc cho `GymBro.API`, image runtime của API cũng chỉ chứa artifact đã publish và ASP.NET runtime thay vì toàn bộ SDK. Image nhỏ hơn sẽ giúp:

- Giảm thời gian build.
- Giảm thời gian push/pull image.
- Giảm dung lượng lưu trữ.
- Tăng tốc triển khai trên môi trường nhiều node.

#### Về hiệu quả bảo mật

Khi chuyển `GymBro.Web` từ single-stage sang secure multi-stage, số lượng packages giảm mạnh từ 1519 xuống còn 178. Điều này dẫn đến việc số lượng lỗ hổng được phát hiện cũng giảm rõ rệt. Đặc biệt, lỗ hổng mức `Critical` đã giảm từ 1 xuống 0. Kết quả Docker Scout có thể thay đổi theo thời điểm cập nhật cơ sở dữ liệu lỗ hổng, nhưng xu hướng chính vẫn rõ: image runtime càng ít thành phần dư thừa thì bề mặt phân tích và số lỗ hổng tiềm ẩn càng giảm.

Ngoài ra, mô hình secure multi-stage áp dụng cho cả `GymBro.Web` và `GymBro.API` còn có thêm hai lợi ích bảo mật quan trọng:

- Không mang theo SDK vào runtime image.
- Không chạy container dưới quyền `root`.
- Không đưa source code, file project, debug symbols và cấu hình development vào final image.
- Không hard-code mật khẩu database và JWT key trong image/compose chính; các giá trị này được truyền qua biến môi trường.

Đây là hai yếu tố rất quan trọng trong hardening container.

#### Về mặt kỹ thuật của dự án GymBro

Sau khi cải tiến, Dockerfile của cả `GymBro.Web` và `GymBro.API` đã được chuẩn hóa theo cùng một hướng:

- Tách rõ các stage `restore`, `build`, `test`, `publish`, `final`.
- Copy đủ các file `.csproj` liên quan, bao gồm `GymBro.Application`, `GymBro.Core`, `GymBro.Infrastructure` và `GymBro.Tests`.
- Chỉ copy artifact từ stage `publish` sang stage `final`.
- Chạy Web bằng user `gymuser` và API bằng user `gymapi`.
- Kết hợp với Docker Compose để chạy filesystem `read_only`, dùng `tmpfs`, `cap_drop: ALL`, `no-new-privileges:true` và volume cho các thư mục cần ghi.

Ngoài hai image ứng dụng, `docker-compose.yml` còn bổ sung service `gymbro_migrator` để chạy EF migration trước khi Web/API khởi động. Service này không còn chạy bằng SDK image chứa source code mà dùng migration bundle trong runtime image riêng. Điều này giúp demo toàn hệ thống chạy được ngay với database `GymBroDB`, thay vì chỉ build image ứng dụng mà không có dữ liệu nền.

Khi kiểm thử bằng `docker-compose.runtime.yml`, hệ thống cũng chạy được mà không cần build lại từ source. Kết quả kiểm tra cho thấy Web trả `200` tại `http://127.0.0.1:5000`, API trả `200` tại `http://127.0.0.1:5001/api/product`, và `gymbro_migrator` kết thúc với exit code `0`.

Nhìn chung, kết quả thực nghiệm đã chứng minh rằng:

**Multi-stage Build không chỉ là kỹ thuật tối ưu dung lượng image mà còn là một giải pháp góp phần tăng cường bảo mật khi triển khai toàn bộ website bán hàng GymBro bằng Docker, bao gồm Web, API, database và luồng khởi tạo database trong Compose.**

## CHƯƠNG 5. KẾT LUẬN VÀ HƯỚNG PHÁT TRIỂN

### 5.1. Kết luận

Đề tài đã nghiên cứu cơ sở lý thuyết về Docker, Docker image, build process và kỹ thuật Multi-stage Build; đồng thời áp dụng trực tiếp trên hệ thống website bán hàng GymBro để xây dựng mô hình đối chứng và mô hình tối ưu cho toàn bộ luồng triển khai bằng Docker.

Kết quả thực nghiệm cho thấy mô hình secure multi-stage có ưu thế rõ rệt so với mô hình single-stage:

- Giảm mạnh kích thước image.
- Giảm số layer.
- Loại bỏ SDK khỏi runtime image.
- Giảm số packages được cài trong image.
- Giảm số lượng lỗ hổng được Docker Scout phát hiện.
- Hỗ trợ chạy container bằng non-root user cho Web, API và migrator.
- Loại bỏ source code, file project, debug symbols và cấu hình development khỏi final image.
- Tách mật khẩu database và JWT key ra khỏi image/compose chính thông qua biến môi trường.
- Hỗ trợ kịch bản chạy bằng image đã build sẵn qua `docker-compose.runtime.yml`, giúp máy triển khai không cần có mã nguồn.

Qua đó có thể khẳng định rằng việc áp dụng Docker Multi-stage Build theo hướng bảo mật cho GymBro là hợp lý và hiệu quả. Đây là hướng tiếp cận phù hợp cho các hệ thống ASP.NET Core có nhiều thành phần như Web, API và database cần tối ưu triển khai nhưng vẫn chú trọng đến bảo mật.

Mặt khác, đề tài cũng cho thấy một kết luận quan trọng:

**Multi-stage Build nên được xem là nền tảng để harden image runtime, chứ không chỉ là một kỹ thuật giảm dung lượng thuần túy.**

Cần lưu ý rằng Multi-stage Build không bảo vệ mã nguồn ở mức tuyệt đối. Final image không còn file source `.cs`, `.csproj` hay `.sln`, nhưng ứng dụng .NET vẫn tồn tại dưới dạng `.dll`, nên người có kỹ thuật cao vẫn có thể decompile để suy luận một phần logic. Vì vậy, kết luận đúng của đề tài là mô hình này giúp **giảm rò rỉ mã nguồn, giảm thông tin dư thừa và giảm attack surface**, không phải đảm bảo không thể phân tích ngược trong mọi tình huống.

### 5.2. Hướng phát triển

Trong thời gian tới, nhóm có thể mở rộng đề tài theo các hướng sau:

- Chuyển secrets từ biến môi trường sang Docker secrets hoặc secret manager nếu triển khai ở môi trường production thật.
- Bổ sung quy trình push image lên private registry để máy triển khai chỉ cần pull image và chạy `docker-compose.runtime.yml`.
- Tích hợp `dotnet test`, build target `final`/`migrator` và kiểm tra endpoint vào CI/CD.
- Tiếp tục tối ưu output publish, ví dụ cân nhắc trimming hoặc self-contained deployment nếu phù hợp với ứng dụng.
- Áp dụng thêm image signing và policy kiểm tra image trước khi deploy.
- Tích hợp quét image tự động trong CI/CD bằng Docker Scout.
- Tạo SBOM và provenance cho image để nâng cao khả năng kiểm soát chuỗi cung ứng phần mềm.

Nếu tiếp tục phát triển theo các hướng trên, GymBro sẽ không chỉ có image gọn hơn mà còn có quy trình đóng gói và triển khai an toàn hơn, phù hợp hơn với yêu cầu của môi trường production thực tế.
