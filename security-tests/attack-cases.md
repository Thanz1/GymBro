# Docker Multi-stage Attack Test Cases

File này mô tả các kịch bản kiểm tra bảo mật dùng trong `run-docker-attack-tests.ps1`. Các kịch bản đều là mô phỏng an toàn, không khai thác phá hoại và không thay đổi dữ liệu nghiệp vụ.

## Nhóm kiểm tra image runtime

| Mã | Kịch bản | Cách kiểm tra | Kỳ vọng với secure multi-stage |
| --- | --- | --- | --- |
| IMG-01 | Runtime user không phải root | Chạy `id -u` trong container tạm từ image | User khác `0` |
| IMG-02 | Không còn .NET SDK trong runtime image | Chạy `dotnet --list-sdks` | Không có SDK nào được liệt kê |
| IMG-03 | Không rò rỉ source code/build file | Tìm `.cs`, `.csproj`, `.sln` trong `/app` và `/src` | Không tìm thấy |
| IMG-04 | Không có file debug/development | Tìm `.pdb` và `appsettings.Development.json` | Không tìm thấy |
| IMG-05 | Không có build tools phổ biến | Kiểm tra `dotnet-ef`, `gcc`, `make`, `git` | Không tìm thấy |
| IMG-06 | Không ghi được vào `/app` khi runtime bị harden | Thử tạo file `.attack_write_test` | Bị chặn nếu compose bật `read_only` hoặc quyền thư mục chặt |
| IMG-07 | Không lộ secrets trong image history | Quét `docker history --no-trunc` | Không có password/key/token |
| IMG-08 | Không lộ secrets trong image environment | Quét `docker image inspect .Config.Env` | Không có password/key/token |

## Nhóm kiểm tra container runtime

| Mã | Kịch bản | Cách kiểm tra | Kỳ vọng với secure deployment |
| --- | --- | --- | --- |
| RUN-01 | Container không chạy root | `docker inspect .Config.User` | Có user non-root |
| RUN-02 | Root filesystem read-only | `docker inspect .HostConfig.ReadonlyRootfs` | `true` với service đã harden |
| RUN-03 | Drop Linux capabilities | `docker inspect .HostConfig.CapDrop` | Có `ALL` |
| RUN-04 | Chặn leo thang đặc quyền | `docker inspect .HostConfig.SecurityOpt` | Có `no-new-privileges:true` |
| RUN-05 | Cổng public được kiểm soát | `docker inspect .NetworkSettings.Ports` | Chỉ service public cần thiết mở cổng |

## Nhóm kiểm tra Docker Compose

| Mã | Kịch bản | Cách kiểm tra | Kỳ vọng |
| --- | --- | --- | --- |
| CMP-01 | Không hard-code secrets trong compose | Quét `docker-compose.yml` | Không có password/key/token trực tiếp |
| CMP-02 | Không dùng môi trường Development khi đánh giá production | Quét `ASPNETCORE_ENVIRONMENT` | Dùng `Production` |
| CMP-03 | Không publish cổng nội bộ quá rộng | Quét port SQL/Rabbit/API | Chỉ publish Web/Gateway nếu triển khai thật |

## Cách đọc kết quả

| Trạng thái | Ý nghĩa |
| --- | --- |
| `PASS` | Đạt yêu cầu bảo mật |
| `FAIL` | Có rủi ro rõ, nên sửa trước khi xem là secure |
| `WARN` | Có rủi ro theo production, nhưng có thể chấp nhận trong demo/dev |
| `SKIP` | Không kiểm tra được vì thiếu image/container/công cụ |
| `INFO` | Thông tin hỗ trợ đánh giá |

Nếu chạy trên baseline single-stage, một số test nên thất bại. Đây là dữ liệu đối chứng để chứng minh secure multi-stage tốt hơn baseline. Nếu chạy trên image secure multi-stage, các test như không có SDK, không có source code, không có build tools và non-root user nên đạt.
