# CHƯƠNG 4. THỰC NGHIỆM VÀ ĐÁNH GIÁ

Chương này trình bày quá trình thực nghiệm trên dự án GymBro nhằm đánh giá hiệu quả của mô hình Docker Secure Multi-stage Build đã đề xuất ở Chương 3. Nội dung thực nghiệm tập trung vào hai nhóm tiêu chí chính: hiệu quả tối ưu hóa image và hiệu quả tăng cường bảo mật container runtime.

Khác với bản thực nghiệm cũ chỉ khảo sát một ứng dụng đơn lẻ, chương này bám vào hiện trạng dự án GymBro tại thư mục gốc của repository. Hệ thống hiện gồm nhiều thành phần: `GymBro.Web`, `GymBro.Gateway`, `GymBro.Identity.API`, `GymBro.Product.API`, `GymBro.Order.API`, các project dùng chung như `GymBro.Core`, `GymBro.Contracts`, `GymBro.Infrastructure`, `GymBro.Service`, cùng hạ tầng `SQL Server` và `RabbitMQ`.

Mục tiêu của chương 4 là trả lời các câu hỏi:

- Mô hình single-stage tạo ra các rủi ro gì trong image runtime?
- Secure multi-stage build có loại bỏ được SDK, source code và build tools khỏi image cuối hay không?
- Việc bổ sung non-root user, read-only filesystem, drop capabilities và no-new-privileges có thể hiện được hiệu quả bảo mật không?
- Hiện trạng `docker-compose.yml` của dự án còn điểm nào cần cải thiện để tiến gần môi trường production?

## 4.1. Thiết lập môi trường thực nghiệm

### 4.1.1. Đối tượng thực nghiệm

Đối tượng thực nghiệm là hệ thống GymBro, một website thương mại điện tử bán đồ gym được phát triển bằng ASP.NET Core/.NET. Dự án đang được tổ chức theo hướng nhiều dịch vụ:

| Thành phần | Vai trò trong hệ thống |
| --- | --- |
| `GymBro.Web` | Giao diện MVC cho người dùng và quản trị viên |
| `GymBro.Gateway` | API Gateway sử dụng Ocelot để định tuyến request |
| `GymBro.Identity.API` | Xác thực, người dùng, SignalR chat, RabbitMQ event, email chào mừng |
| `GymBro.Product.API` | Sản phẩm, danh mục, đánh giá và các nghiệp vụ liên quan catalog |
| `GymBro.Order.API` | Giỏ hàng, đơn hàng, thanh toán, wishlist và nghiệp vụ đặt hàng |
| `SQL Server` | Lưu trữ dữ liệu cho các service |
| `RabbitMQ` | Message broker cho event bất đồng bộ |

Các Dockerfile ở dự án root hiện đã sử dụng multi-stage build cơ bản, gồm stage build bằng `mcr.microsoft.com/dotnet/sdk:8.0` và stage runtime bằng `mcr.microsoft.com/dotnet/aspnet:8.0`. Trong phần thực nghiệm, nhóm sử dụng thêm các image nghiên cứu đã build sẵn để so sánh giữa baseline single-stage và secure multi-stage.

### 4.1.2. Môi trường phần cứng, phần mềm

Quá trình đo đạc được thực hiện trên cùng một máy phát triển nhằm đảm bảo kết quả có thể đối chiếu nhất quán.

| Nhóm thông tin | Giá trị |
| --- | --- |
| Hệ điều hành | Microsoft Windows 10 IoT Enterprise LTSC 10.0.19044 |
| Kiến trúc máy | x64-based PC, 64-bit |
| Docker Engine/Client | Docker 28.5.1 |
| .NET SDK cài trên máy | 9.0.315 |
| PowerShell | 5.1.19041.7417 |
| Công cụ tạo báo cáo test | `security-tests/run-docker-attack-tests.ps1` |
| Solution chính | `GymBro-multi.sln` |
| Compose chính | `docker-compose.yml` |

Lưu ý: các project ở root của GymBro dùng `net8.0`. Việc máy phát triển có .NET SDK 9.0.315 không làm thay đổi mục tiêu thực nghiệm, vì tiêu chí chính ở đây là kiểm tra image runtime sau khi build, không phải đánh giá tính năng của framework.

### 4.1.3. Các image được đưa vào thực nghiệm

Bộ thực nghiệm sử dụng hai nhóm image:

- **Baseline single-stage:** image đối chứng, cố tình giữ lại SDK, source code và công cụ build trong runtime.
- **Secure multi-stage:** image đã tách build/runtime và bổ sung hardening như non-root user, read-only rootfs, `cap_drop: ALL`, `no-new-privileges:true`.

| Image | Vai trò |
| --- | --- |
| `gymbro:single-stage` | Baseline single-stage tổng hợp |
| `gymbro-web-single-report:latest` | Baseline single-stage cho Web dùng trong báo cáo |
| `gymbro/web:secure-multistage` | Secure multi-stage cho Web |
| `gymbro/api:secure-multistage` | Secure multi-stage cho API |
| `gymbro/migrator:secure-multistage` | Migration bundle/runtime image |
| `gymbro:secure` | Secure image dùng trong demo kiểm thử |
| `gymbro-web-multistage-report:latest` | Multi-stage image cho Web dùng trong báo cáo |

## 4.2. Phương pháp thực nghiệm

### 4.2.1. Mô hình đối chứng và mô hình đề xuất

Thực nghiệm được thiết kế theo hướng so sánh đối chứng:

| Mô hình | Mục đích | Đặc điểm |
| --- | --- | --- |
| Baseline single-stage | Làm mốc so sánh | Dùng SDK image cho cả build và runtime, có nguy cơ chứa source code và build tools |
| Secure multi-stage | Mô hình đề xuất | Tách `restore/build/publish/final`, chỉ copy artifact đã publish, chạy non-root, hardening runtime |

Việc so sánh không chỉ dựa trên kích thước image mà còn dựa trên các dấu hiệu bảo mật có thể kiểm chứng được trong container runtime.

### 4.2.2. Bộ kiểm thử tấn công an toàn

Nhóm tạo bộ kiểm thử tại thư mục `security-tests/` gồm:

| File | Chức năng |
| --- | --- |
| `run-docker-attack-tests.ps1` | Script chạy kiểm tra image, container và Compose |
| `docker-attack-targets.json` | Danh sách image/container cần kiểm tra |
| `attack-cases.md` | Mô tả các kịch bản kiểm thử |
| `reports/docker-attack-report-20260618-191833.md` | Báo cáo kết quả kiểm thử đã sinh |

Các kịch bản kiểm thử là mô phỏng an toàn, không khai thác phá hoại và không làm thay đổi dữ liệu nghiệp vụ của hệ thống. Các kiểm tra chính gồm:

| Mã | Nội dung kiểm tra | Mục tiêu bảo mật |
| --- | --- | --- |
| `IMG-01` | Kiểm tra runtime user bằng `id -u` | Container không chạy bằng `root` |
| `IMG-02` | Chạy `dotnet --list-sdks` | Runtime image không chứa .NET SDK |
| `IMG-03` | Tìm `.cs`, `.csproj`, `.sln` | Không rò rỉ source code/build file |
| `IMG-04` | Tìm `.pdb`, `appsettings.Development.json` | Không đưa file debug/development vào runtime |
| `IMG-05` | Kiểm tra `dotnet-ef`, `gcc`, `make`, `git` | Không chứa build tools phổ biến |
| `IMG-06` | Thử ghi file vào `/app` | Kiểm tra quyền ghi runtime |
| `IMG-07` | Quét `docker history --no-trunc` | Không lộ secrets trong image layers |
| `IMG-08` | Quét `docker image inspect .Config.Env` | Không lộ secrets trong image environment |
| `RUN-01` | Kiểm tra `Config.User` | Container runtime chạy non-root |
| `RUN-02` | Kiểm tra `ReadonlyRootfs` | Root filesystem read-only |
| `RUN-03` | Kiểm tra `CapDrop` | Drop Linux capabilities không cần thiết |
| `RUN-04` | Kiểm tra `SecurityOpt` | Bật `no-new-privileges` |
| `CMP-01` | Quét secrets trong Compose | Không hard-code mật khẩu/key trong compose |
| `CMP-02` | Kiểm tra môi trường chạy | Không dùng `Development` cho secure deploy |
| `CMP-03` | Kiểm tra port nội bộ | Không publish quá nhiều cổng nội bộ |

Lệnh chạy thực nghiệm:

```powershell
powershell -ExecutionPolicy Bypass -File .\security-tests\run-docker-attack-tests.ps1 -Profile research
```

Kết quả được ghi vào:

```text
security-tests/reports/docker-attack-report-20260618-191833.md
```

## 4.3. Kết quả thực nghiệm về kích thước image và số layer

### 4.3.1. Kích thước image

Kích thước image được lấy từ `docker image inspect .Size` trong script kiểm thử để đảm bảo cùng một phương pháp đo cho tất cả image. Kết quả như sau:

| Image | Mô hình | Kích thước |
| --- | --- | ---: |
| `gymbro:single-stage` | Baseline single-stage | 629.1 MB |
| `gymbro-web-single-report:latest` | Baseline Web single-stage | 616.8 MB |
| `gymbro/web:secure-multistage` | Secure Web multi-stage | 110.6 MB |
| `gymbro/api:secure-multistage` | Secure API multi-stage | 102.1 MB |
| `gymbro/migrator:secure-multistage` | Secure migrator | 111.4 MB |
| `gymbro:secure` | Secure demo image | 123.7 MB |
| `gymbro-web-multistage-report:latest` | Web multi-stage report image | 111.2 MB |

Mức giảm kích thước image được tính theo công thức:

```text
Mức giảm (%) = (Kích thước baseline - Kích thước secure) / Kích thước baseline * 100
```

So sánh trực tiếp giữa `gymbro:single-stage` và `gymbro/web:secure-multistage`:

```text
(629.1 - 110.6) / 629.1 * 100 = 82.42%
```

So sánh riêng cặp Web report:

```text
(616.8 - 111.2) / 616.8 * 100 = 81.97%
```

Như vậy, image Web sau khi chuyển sang secure multi-stage giảm xấp xỉ 82% so với baseline single-stage. Đây là mức giảm đáng kể, cho thấy phần lớn dung lượng của baseline không đến từ mã nguồn ứng dụng mà đến từ SDK, công cụ build, cache và các thành phần không cần cho runtime.

### 4.3.2. Số layer image

Số layer được đo bằng `docker history`.

| Image | Số layer |
| --- | ---: |
| `gymbro:single-stage` | 22 |
| `gymbro-web-single-report:latest` | 21 |
| `gymbro/web:secure-multistage` | 17 |
| `gymbro/api:secure-multistage` | 17 |
| `gymbro/migrator:secure-multistage` | 15 |
| `gymbro:secure` | 17 |
| `gymbro-web-multistage-report:latest` | 15 |

Kết quả cho thấy secure multi-stage không chỉ làm image nhỏ hơn mà còn giảm số layer runtime. Tuy số layer không phải tiêu chí bảo mật duy nhất, việc giảm layer thừa giúp image dễ kiểm soát hơn và giảm khả năng mang theo file hoặc công cụ không cần thiết.

## 4.4. Kết quả kiểm thử tấn công image runtime

### 4.4.1. Tổng quan kết quả

Report kiểm thử sinh ra tổng cộng 93 dòng kết quả.

| Trạng thái | Số lượng | Ý nghĩa |
| --- | ---: | --- |
| `PASS` | 54 | Đạt yêu cầu kiểm tra |
| `WARN` | 15 | Có rủi ro cần cân nhắc trong production |
| `FAIL` | 10 | Phát hiện rủi ro rõ ràng |
| `INFO` | 12 | Thông tin hỗ trợ đánh giá |
| `SKIP` | 2 | Không kiểm tra được do thiếu container cụ thể |

Các `FAIL` chủ yếu xuất hiện ở baseline single-stage và ở file `docker-compose.yml` hiện tại. Trong khi đó, các image secure multi-stage đạt hầu hết kiểm tra quan trọng như non-root user, không có SDK, không có source code, không có build tools và không chứa secrets trong image history/env.

### 4.4.2. Kết quả trên baseline single-stage

Khi kiểm tra `gymbro:single-stage`, các lỗi chính được phát hiện:

| Kịch bản | Kết quả | Bằng chứng |
| --- | --- | --- |
| `IMG-01` Runtime user non-root | `FAIL` | `uid=0 user=root` |
| `IMG-02` Không còn .NET SDK | `FAIL` | Có SDK `9.0.313 [/usr/share/dotnet/sdk]` |
| `IMG-03` Không leak source/build file | `FAIL` | Tìm thấy nhiều file `.cs` trong `/src/GymBro.Web/Controllers/...` |
| `IMG-04` Không có file debug/development | `WARN` | Có `.pdb` và `appsettings.Development.json` |
| `IMG-05` Không có build tools | `FAIL` | Tìm thấy `/usr/bin/git` |
| `RUN-01` Container user non-root | `FAIL` | `Config.User=''`, tức là không khai báo user runtime |
| `RUN-02` Root filesystem read-only | `WARN` | `ReadonlyRootfs=false` |
| `RUN-03` Drop capabilities | `WARN` | `CapDrop=null` |
| `RUN-04` No-new-privileges | `WARN` | `SecurityOpt=null` |

Các kết quả trên cho thấy baseline single-stage tồn tại nhiều rủi ro:

- Runtime image vẫn chứa SDK, tức là container có nhiều công cụ hơn mức cần thiết.
- Source code còn tồn tại trong image, làm tăng nguy cơ lộ logic nghiệp vụ nếu image bị truy cập trái phép.
- Container chạy với quyền root hoặc không khai báo user rõ ràng.
- Một số file debug/development còn trong image, không phù hợp với production.
- Build tools như `git` vẫn tồn tại, làm tăng attack surface.

### 4.4.3. Kết quả trên secure multi-stage

Đối với `gymbro/web:secure-multistage`, các kiểm tra quan trọng đều đạt:

| Kịch bản | Kết quả | Bằng chứng |
| --- | --- | --- |
| `IMG-01` Runtime user non-root | `PASS` | `uid=1000 user=gymuser` |
| `IMG-02` Không còn .NET SDK | `PASS` | `dotnet --list-sdks` không trả SDK |
| `IMG-03` Không leak source/build file | `PASS` | Không tìm thấy `.cs`, `.csproj`, `.sln` trong `/app` hoặc `/src` |
| `IMG-04` Không có file debug/development | `PASS` | Không tìm thấy `.pdb` hoặc `appsettings.Development.json` |
| `IMG-05` Không có build tools | `PASS` | Không tìm thấy `dotnet-ef`, `gcc`, `make`, `git` |
| `IMG-07` Không lộ secrets trong image history | `PASS` | Không phát hiện secret pattern |
| `IMG-08` Không lộ secrets trong image env | `PASS` | Không phát hiện secret pattern |

Đối với `gymbro/api:secure-multistage`, kết quả tương tự:

| Kịch bản | Kết quả | Bằng chứng |
| --- | --- | --- |
| `IMG-01` Runtime user non-root | `PASS` | `uid=1000 user=gymapi` |
| `IMG-02` Không còn .NET SDK | `PASS` | Không có SDK trong runtime |
| `IMG-03` Không leak source/build file | `PASS` | Không tìm thấy source/build file |
| `IMG-05` Không có build tools | `PASS` | Không tìm thấy build tools phổ biến |
| `IMG-07`, `IMG-08` Secrets | `PASS` | Không phát hiện secrets trong history/env |

Kết quả này chứng minh mô hình secure multi-stage đã giải quyết trực tiếp các rủi ro chính của baseline:

- SDK chỉ nằm ở stage build, không xuất hiện trong final image.
- Source code không được copy sang image runtime.
- Runtime image không chứa build tools.
- Container chạy bằng user thường thay vì root.
- Image không bake secrets vào layer hoặc environment mặc định.

### 4.4.4. Kiểm tra quyền ghi vào `/app`

Kịch bản `IMG-06` thử tạo file `.attack_write_test` trong `/app`. Một số image secure trả về `WARN` với bằng chứng `WRITABLE`.

Điều này không phủ nhận hiệu quả của multi-stage build, vì Dockerfile chỉ kiểm soát nội dung và user của image. Để root filesystem thực sự read-only, cần áp dụng hardening ở mức container runtime bằng Docker Compose:

```yaml
read_only: true
tmpfs:
  - /tmp
```

Trong report, các container secure đã harden ở runtime có kết quả:

| Kịch bản | Kết quả secure runtime |
| --- | --- |
| `RUN-02` Root filesystem read-only | `PASS` |
| `RUN-03` Linux capabilities dropped | `PASS` |
| `RUN-04` No-new-privileges enabled | `PASS` |

Như vậy, bài học rút ra là bảo mật container cần kết hợp cả Dockerfile và cấu hình runtime. Multi-stage build giúp image sạch hơn, còn Compose hardening giúp container bị giới hạn quyền khi vận hành.

## 4.5. Kết quả kiểm tra Docker Compose

Ngoài image, script cũng kiểm tra `docker-compose.yml` ở root. Kết quả cho thấy compose hiện tại vẫn là cấu hình thiên về demo/development, chưa phải cấu hình production.

| Kịch bản | Trạng thái | Bằng chứng |
| --- | --- | --- |
| `CMP-01` Không hard-code secrets | `FAIL` | Có `MSSQL_SA_PASSWORD`, `RABBITMQ_DEFAULT_PASS`, connection string chứa password |
| `CMP-02` Không dùng Development | `WARN` | Có `ASPNETCORE_ENVIRONMENT=Development` |
| `CMP-03` Không publish port nội bộ quá rộng | `WARN` | Publish `1433`, `5672`, `15672`, `7001`, `7002`, `7003` |

Các phát hiện này phản ánh đúng hiện trạng dự án:

- `docker-compose.yml` đang để mật khẩu SQL Server và RabbitMQ trực tiếp trong file.
- Các service ứng dụng đang chạy ở môi trường `Development`.
- SQL Server, RabbitMQ management và các API nội bộ đều được publish ra host để tiện demo.

Đây là rủi ro ở mức cấu hình triển khai, không phải lỗi của multi-stage build. Tuy nhiên, nếu mục tiêu là production, cần cải tiến theo hướng:

- Đưa secrets sang `.env`, Docker secrets hoặc secret manager.
- Dùng `ASPNETCORE_ENVIRONMENT=Production`.
- Chỉ publish `GymBro.Web` hoặc `GymBro.Gateway`; các API, SQL Server và RabbitMQ nên nằm trong Docker network nội bộ.
- Thêm `read_only: true`, `tmpfs`, `cap_drop: ALL`, `security_opt: no-new-privileges:true` cho các service ứng dụng.
- Thêm healthcheck để kiểm soát thứ tự khởi động và smoke test.

## 4.6. Phân tích và đánh giá

### 4.6.1. Đánh giá hiệu quả giảm attack surface

Attack surface của container phụ thuộc nhiều vào những thành phần tồn tại trong image runtime. Với baseline single-stage, image chứa SDK, source code, file debug, build tools và chạy với quyền root. Đây là các yếu tố làm tăng rủi ro nếu container bị truy cập trái phép.

Secure multi-stage build đã làm giảm attack surface theo các hướng:

| Yếu tố rủi ro | Baseline single-stage | Secure multi-stage |
| --- | --- | --- |
| .NET SDK trong runtime | Có | Không |
| Source code trong runtime | Có | Không |
| Build tools | Có `git` | Không phát hiện |
| User runtime | `root` hoặc không khai báo | Non-root user |
| Debug/development file | Có `.pdb`, `appsettings.Development.json` | Không phát hiện ở image secure chính |
| Secrets trong image history/env | Không phát hiện | Không phát hiện |

Từ bảng trên, có thể kết luận rằng secure multi-stage build không chỉ làm image nhỏ hơn mà còn loại bỏ nhiều thành phần có thể bị lợi dụng khi tấn công.

### 4.6.2. Đánh giá hiệu quả vận hành

Về vận hành, image nhỏ hơn mang lại các lợi ích:

- Pull image nhanh hơn khi triển khai trên máy mới.
- Push image lên registry nhanh hơn.
- Tiết kiệm dung lượng lưu trữ image.
- Giảm số thành phần cần quét khi kiểm tra lỗ hổng.
- Dễ kiểm soát nội dung image runtime hơn.

Mức giảm khoảng 82% giữa baseline single-stage và secure multi-stage Web image là đủ lớn để chứng minh giá trị thực tế của mô hình. Với hệ thống nhiều service như GymBro, nếu mỗi service đều được tối ưu tương tự, lợi ích tích lũy khi build, push, pull và deploy sẽ rõ rệt hơn.

### 4.6.3. Đánh giá theo câu hỏi nghiên cứu

**RQ1: Multi-stage build giúp giảm bề mặt tấn công của Docker image như thế nào?**

Kết quả thực nghiệm cho thấy multi-stage build loại bỏ SDK, source code và build tools khỏi image cuối. Đây là các thành phần không cần thiết cho runtime nhưng lại làm tăng bề mặt tấn công. Image secure multi-stage của Web và API đều pass các kiểm tra `IMG-02`, `IMG-03`, `IMG-05`.

**RQ2: Khi kết hợp multi-stage build với hardening, mức độ an toàn container cải thiện ra sao?**

Khi kết hợp với non-root user, read-only rootfs, `cap_drop: ALL` và `no-new-privileges:true`, các container secure đạt kết quả `PASS` ở các kiểm tra runtime `RUN-01`, `RUN-02`, `RUN-03`, `RUN-04`. Điều này chứng minh bảo mật container cần được xử lý đồng thời ở image và runtime.

**RQ3: Các cải tiến bảo mật có ảnh hưởng đến khả năng vận hành ứng dụng không?**

Trong phạm vi thực nghiệm, các image secure vẫn build và được kiểm tra runtime thành công. Việc giảm image size và loại bỏ SDK không làm mất khả năng chạy ứng dụng, vì ASP.NET Core runtime vẫn đủ để thực thi artifact đã publish. Một số thư mục cần ghi như `/tmp`, Data Protection Keys hoặc thư mục upload ảnh cần được mount volume rõ ràng khi bật `read_only`.

## 4.7. Hạn chế của thực nghiệm

Thực nghiệm đã chứng minh được hiệu quả của secure multi-stage build ở mức image và container runtime, nhưng vẫn còn một số giới hạn:

- Chưa tích hợp đầy đủ vào CI/CD tự động.
- Chưa chạy quét CVE bằng Trivy/Docker Scout trong report cuối cùng.
- Chưa bổ sung endpoint `/health` chính thức cho tất cả service.
- Chưa đo thời gian build/push/pull ở nhiều lần chạy để có số liệu trung bình.
- `docker-compose.yml` root hiện vẫn là cấu hình demo/development, chưa phải production compose hoàn chỉnh.
- Một số image thực nghiệm là image nghiên cứu đã build trước đó, dùng để so sánh bảo mật image, chưa thay thế toàn bộ Dockerfile root hiện tại.

Các giới hạn này không làm mất giá trị của kết quả chính, vì mục tiêu của chương là chứng minh tác động của single-stage so với secure multi-stage ở mức image/runtime. Tuy nhiên, để triển khai production thật, cần tiếp tục hoàn thiện cấu hình Compose và pipeline.

## 4.8. Đề xuất cải tiến tiếp theo

Dựa trên kết quả thực nghiệm, nhóm đề xuất các cải tiến sau:

1. Chuẩn hóa Dockerfile của tất cả service root theo mô hình `restore -> build -> test -> publish -> final`.
2. Thêm non-root user vào final stage cho `GymBro.Web`, `GymBro.Gateway`, `GymBro.Identity.API`, `GymBro.Product.API`, `GymBro.Order.API`.
3. Tách secrets khỏi `docker-compose.yml`, sử dụng `.env` hoặc Docker secrets.
4. Chuyển cấu hình đánh giá bảo mật sang `ASPNETCORE_ENVIRONMENT=Production`.
5. Chỉ publish Web/Gateway trong production; giữ API, SQL Server và RabbitMQ trong Docker network nội bộ.
6. Bật `read_only: true`, `tmpfs`, `cap_drop: ALL`, `no-new-privileges:true` cho service ứng dụng.
7. Thêm healthcheck cho từng service.
8. Đưa `security-tests/run-docker-attack-tests.ps1` vào pipeline CI/CD.
9. Bổ sung Trivy hoặc Docker Scout để quét CVE tự động.
10. Sinh SBOM và cân nhắc image signing nếu triển khai trên registry production.

## 4.9. Kết luận chương

Chương 4 đã thực hiện kiểm chứng mô hình Docker Secure Multi-stage Build trên dự án GymBro. Kết quả cho thấy mô hình secure multi-stage giúp giảm mạnh kích thước image, loại bỏ SDK khỏi runtime, không còn source code/build file trong final image, không chứa build tools phổ biến và cho phép chạy container với user thường.

So với baseline single-stage, image secure multi-stage của Web giảm khoảng 82% kích thước. Các kiểm thử tấn công an toàn cũng cho thấy baseline thất bại ở nhiều điểm quan trọng như chạy root, còn SDK, leak source code và còn build tools. Ngược lại, các image secure multi-stage đạt hầu hết tiêu chí bảo mật ở mức image.

Kết quả kiểm tra Docker Compose cũng chỉ ra các điểm cần cải thiện trong triển khai thực tế: không hard-code secrets, không dùng môi trường Development khi đánh giá production và không publish quá nhiều cổng nội bộ. Những phát hiện này giúp hoàn thiện hướng nghiên cứu: multi-stage build là nền tảng, nhưng để đạt mức bảo mật tốt hơn cần kết hợp thêm hardening ở runtime và quản lý cấu hình triển khai.

Từ kết quả trên, có thể khẳng định rằng mô hình Docker Secure Multi-stage Build là phù hợp với GymBro và có giá trị thực tiễn đối với các dự án ASP.NET Core/.NET nhiều service tương tự.
