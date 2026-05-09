# Định hướng đề tài nghiên cứu khoa học về Docker

## 1. Vì sao nhóm đang thấy "multistage" hơi nhỏ

Nếu đề tài chỉ dừng ở mức **"áp dụng Docker image multistage"** thì đúng là hơi hẹp, vì:

- Multistage chủ yếu là một **kỹ thuật tối ưu Dockerfile**.
- Giá trị dễ thấy nhất của nó thường là **giảm kích thước image** và **tách môi trường build với môi trường runtime**.
- Nếu chỉ mô tả cách viết `FROM ... AS build` rồi `COPY --from=build`, đề tài dễ bị nhìn như một bài thực hành kỹ thuật hơn là một nghiên cứu có chiều sâu.

Nói ngắn gọn:

**Multistage là công cụ, chưa phải là bài toán nghiên cứu đủ lớn.**

Muốn đề tài "đủ lực", nhóm nên trả lời một câu hỏi lớn hơn:

**Dùng multistage để giải quyết vấn đề gì trong thực tế?**

Hướng mà thầy gợi ý là hoàn toàn hợp lý:

**Dùng multistage như nền tảng để tăng cường bảo mật container image và môi trường triển khai.**

## 2. Cách hiểu đúng hướng đề tài

Nhóm không nên nghĩ đề tài mới là "bỏ multistage để làm bảo mật".

Nên hiểu như sau:

- **Multistage build** là phần lõi kỹ thuật.
- **Bảo mật container** là mục tiêu nghiên cứu.
- **Đánh giá hiệu quả** mới là phần làm cho đề tài có tính khoa học.

Nói cách khác:

**Multistage là phương tiện, bảo mật là mục tiêu, còn đo đạc so sánh là phần nghiên cứu.**

## 3. Tên đề tài nên chốt theo hướng nào

Nhóm có thể chọn một trong các tên sau:

### Phương án 1

**Nghiên cứu áp dụng kỹ thuật Docker image multistage nhằm tăng cường bảo mật cho container triển khai ứng dụng .NET**

### Phương án 2

**Xây dựng và đánh giá mô hình hardening Docker image dựa trên multistage build cho ứng dụng web .NET**

### Phương án 3

**Ứng dụng Docker multistage kết hợp các kỹ thuật hardening để giảm bề mặt tấn công của container**

Nếu muốn cân bằng giữa dễ làm và vẫn đủ học thuật, nhóm nên chọn:

**"Nghiên cứu áp dụng kỹ thuật Docker image multistage nhằm tăng cường bảo mật cho container triển khai ứng dụng .NET"**

## 4. Vấn đề nghiên cứu mà nhóm có thể nêu

Khi triển khai ứng dụng bằng Docker, nhiều image vẫn tồn tại các rủi ro như:

- Chứa thừa **SDK, compiler, package manager, source code** trong image runtime.
- Container chạy bằng **quyền root**.
- Image có nhiều thành phần không cần thiết, làm tăng **attack surface**.
- Thông tin nhạy cảm như mật khẩu, connection string bị để trực tiếp trong cấu hình chạy container.
- Chưa có cơ chế kiểm tra và đánh giá lỗ hổng của image trước khi triển khai.

Từ đó, nhóm có thể đặt bài toán:

**Làm thế nào để sử dụng multistage build kết hợp hardening nhằm tạo ra image gọn hơn, ít lỗ hổng hơn, khó bị khai thác hơn nhưng vẫn đảm bảo ứng dụng chạy ổn định?**

## 5. Nhìn vào chính dự án hiện tại của nhóm

Qua codebase hiện tại, nhóm đã có một nền khá tốt để làm đề tài:

- `GymBro.API/Dockerfile` đã dùng **3 stage**: `build`, `publish`, `final`.
- `GymBro.Web/Dockerfile` cũng đã dùng multistage và đã có bước **chạy non-root user**.
- `docker-compose.yml` đang cho `GymBro.API` và `GymBro.Web` chạy ở `ASPNETCORE_ENVIRONMENT=Development`.
- `docker-compose.yml` đang chứa trực tiếp **mật khẩu SA** và **connection string** trong file compose.
- `GymBro.API/Dockerfile` hiện chưa có bước hardening rõ như `USER non-root`.

Điều này rất tốt cho nghiên cứu, vì nhóm có thể tạo ra hai mốc so sánh rõ ràng:

- **Mô hình cơ sở (baseline)**: trạng thái Docker hiện tại của dự án.
- **Mô hình cải tiến (secure multistage)**: bản Dockerfile/Compose sau khi áp dụng hardening bảo mật.

Đây chính là điểm làm cho đề tài có chiều sâu: **không nói lý thuyết suông, mà đo trên hệ thống thật của nhóm**.

## 6. Mục tiêu nghiên cứu

### Mục tiêu tổng quát

Nghiên cứu và áp dụng kỹ thuật Docker image multistage kết hợp hardening container để nâng cao mức độ bảo mật khi triển khai ứng dụng .NET bằng Docker.

### Mục tiêu cụ thể

- Phân tích vai trò của multistage build trong việc giảm thành phần dư thừa của image.
- Xây dựng bộ Dockerfile theo hướng bảo mật hơn cho hệ thống hiện tại.
- Giảm quyền thực thi của container bằng cách chạy dưới tài khoản non-root.
- Hạn chế rò rỉ bí mật cấu hình khi triển khai bằng Docker Compose.
- Đánh giá trước và sau cải tiến theo các chỉ số cụ thể như kích thước image, số lỗ hổng, quyền thực thi và khả năng chống cấu hình sai.

## 7. Câu hỏi nghiên cứu

Nhóm có thể dùng 3 câu hỏi nghiên cứu sau:

### RQ1

Multistage build giúp giảm bề mặt tấn công của Docker image như thế nào?

### RQ2

Khi kết hợp multistage build với các kỹ thuật hardening như non-root user, image tối giản và quản lý secrets tốt hơn, mức độ an toàn của container cải thiện ra sao?

### RQ3

Các cải tiến bảo mật đó có ảnh hưởng đáng kể đến khả năng build, triển khai và vận hành ứng dụng hay không?

## 8. Giả thuyết nghiên cứu

Nhóm có thể viết giả thuyết theo kiểu đơn giản và chắc:

**Việc áp dụng Docker multistage kết hợp các kỹ thuật hardening phù hợp sẽ làm giảm kích thước image, giảm số thành phần dư thừa, giảm rủi ro chạy container với đặc quyền cao và từ đó nâng cao mức độ bảo mật triển khai ứng dụng so với cách đóng gói container thông thường.**

## 9. Nhóm nên triển khai những gì để đề tài đủ mạnh

Đây là phần quan trọng nhất. Nhóm không cần ôm quá nhiều thứ. Chỉ cần làm tốt một trục chính:

**Từ multistage build tiến đến secure container image.**

### 9.1. Chuẩn hóa multistage build

Nên tách rõ các stage:

- `restore`
- `build`
- `test` nếu nhóm có test
- `publish`
- `final`

Ý nghĩa nghiên cứu:

- Giảm việc đưa công cụ build vào image runtime.
- Chỉ mang artifact cần thiết sang image cuối.
- Giảm số package và file thừa tồn tại trong image chạy thật.

### 9.2. Harden image runtime

Các hạng mục nhóm nên làm:

- Chạy cả `GymBro.API` và `GymBro.Web` bằng **non-root user**.
- Dùng runtime image tối giản hơn nếu ứng dụng tương thích.
- Chỉ copy file publish cần thiết, không copy source code, file tạm, cache build.
- Xem xét pin phiên bản base image rõ ràng hơn để tránh drift môi trường.

### 9.3. Bảo vệ secrets và cấu hình nhạy cảm

Hiện tại file `docker-compose.yml` đang để thẳng:

- mật khẩu `sa`
- connection string
- môi trường `Development`

Đây là một điểm nghiên cứu rất tốt. Nhóm nên cải tiến theo hướng:

- Tách cấu hình dev và prod.
- Không hard-code mật khẩu trong compose chính.
- Dùng `.env`, file cấu hình nội bộ không commit, hoặc `Docker secrets` nếu phạm vi đề tài cho phép.
- Chuyển sang cấu hình gần production hơn khi đánh giá bảo mật.

### 9.4. Runtime hardening

Nếu nhóm muốn đề tài nổi bật hơn, có thể thêm các biện pháp sau ở mức vừa phải:

- `read_only: true` cho filesystem nếu ứng dụng phù hợp.
- `tmpfs` cho vùng ghi tạm.
- Giảm Linux capabilities nếu không cần.
- `security_opt: no-new-privileges:true`.
- Giới hạn network và volume theo nguyên tắc **least privilege**.

Không bắt buộc phải làm tất cả. Chỉ cần chọn vài biện pháp tiêu biểu và đo hiệu quả rõ ràng.

### 9.5. Quét lỗ hổng và phân tích image

Để đề tài có tính đo đạc, nhóm nên bổ sung bước:

- Quét lỗ hổng image trước và sau cải tiến.
- So sánh số lượng `Critical`, `High`, `Medium` vulnerabilities.
- Kiểm tra trong image cuối còn gì: shell, compiler, package manager, source code hay không.

### 9.6. Hướng nâng cao nếu còn thời gian

Nếu nhóm còn thời gian và muốn tăng chất lượng học thuật, có thể thêm:

- SBOM cho image.
- Provenance/attestation trong quá trình build.
- Quy trình CI/CD kiểm tra bảo mật image trước khi push.

Phần này là **điểm cộng**, không nên để thành phần bắt buộc nếu thời gian ngắn.

## 10. Phạm vi đề tài nên giới hạn thế nào

Để tránh bị loãng, nhóm nên giới hạn phạm vi như sau:

- Tập trung vào **bảo mật ở mức image và container runtime**.
- Thực nghiệm trên chính hệ thống `.NET` hiện tại gồm `GymBro.API`, `GymBro.Web` và `SQL Server` chạy bằng Docker Compose.
- Không mở rộng quá xa sang Kubernetes, service mesh, zero trust hay bảo mật hạ tầng cloud nếu chưa đủ thời gian.

Nói cách khác:

**Đề tài nên sâu ở một lát cắt, không nên rộng quá nhiều lát cắt.**

## 11. Bộ tiêu chí đánh giá nên dùng

Đây là phần giúp đề tài có "chất nghiên cứu". Nhóm nên lập bảng so sánh trước và sau cải tiến.

| Tiêu chí | Mô hình cơ sở | Mô hình cải tiến |
| --- | --- | --- |
| Kích thước image | Đo theo MB | Đo theo MB |
| Số layer | Thống kê | Thống kê |
| Có chạy bằng root không | Có/Không | Có/Không |
| Có chứa SDK, source code, file build thừa không | Có/Không | Có/Không |
| Số lỗ hổng Critical/High/Medium | Thống kê | Thống kê |
| Secrets có bị hard-code trong compose không | Có/Không | Có/Không |
| Mức độ tối giản của image runtime | Thấp/Trung bình/Cao | Thấp/Trung bình/Cao |
| Khả năng chạy ổn định của ứng dụng | Đạt/Không đạt | Đạt/Không đạt |

Nếu muốn viết theo hướng khoa học hơn, nhóm có thể chia kết quả thành 3 nhóm:

- **Hiệu quả bảo mật**
- **Hiệu quả tài nguyên**
- **Ảnh hưởng đến vận hành**

## 12. Phương pháp thực nghiệm gợi ý

Nhóm có thể làm theo quy trình này:

### Bước 1. Xây dựng mô hình cơ sở

- Dùng Dockerfile và Compose hiện tại.
- Build và chạy hệ thống.
- Ghi nhận kích thước image, cấu trúc stage, quyền user, cách quản lý secrets.

### Bước 2. Xây dựng mô hình cải tiến

- Refactor Dockerfile theo hướng multistage chặt chẽ hơn.
- Chuyển container sang non-root.
- Tối giản image final.
- Tách hoặc bảo vệ thông tin nhạy cảm trong Compose.
- Bổ sung một số thiết lập hardening ở runtime.

### Bước 3. Đánh giá và so sánh

- So sánh image size.
- So sánh số lỗ hổng.
- So sánh quyền thực thi.
- Kiểm tra khả năng chạy ổn định sau khi harden.

### Bước 4. Rút ra kết luận

- Biện pháp nào hiệu quả nhất.
- Biện pháp nào có tác dụng nhưng gây bất tiện vận hành.
- Mức độ phù hợp của multistage trong bảo mật image cho ứng dụng .NET.

## 13. Cách viết bố cục báo cáo

Nhóm có thể viết báo cáo theo bố cục này:

### Chương 1. Tổng quan

- Docker và containerization
- Docker image
- Multistage build
- Các rủi ro bảo mật khi đóng gói và triển khai container

### Chương 2. Cơ sở lý thuyết

- Nguyên lý hoạt động của Dockerfile nhiều giai đoạn
- Attack surface trong container
- Least privilege
- Non-root container
- Secrets management
- Vulnerability scanning

### Chương 3. Phân tích hệ thống thực nghiệm

- Giới thiệu dự án GymBro
- Kiến trúc API, Web, SQL Server
- Hiện trạng Dockerfile và Compose
- Các điểm còn hạn chế về bảo mật

### Chương 4. Xây dựng mô hình cải tiến

- Thiết kế Dockerfile multistage mới
- Thiết kế cấu hình Compose an toàn hơn
- Các biện pháp hardening được áp dụng

### Chương 5. Thực nghiệm và đánh giá

- Môi trường thử nghiệm
- Bộ tiêu chí đánh giá
- Kết quả trước và sau cải tiến
- Phân tích kết quả

### Chương 6. Kết luận và hướng phát triển

- Kết luận chính
- Hạn chế đề tài
- Hướng mở rộng như SBOM, CI/CD security, image signing

## 14. Những điểm nhóm nên tránh

Để đề tài không bị loãng hoặc bị đánh giá là chỉ làm demo, nhóm nên tránh:

- Chỉ trình bày cách viết multistage mà không có đo đạc so sánh.
- Nói quá rộng về bảo mật Docker nhưng không gắn với hệ thống thật.
- Chạy theo quá nhiều công nghệ nâng cao cùng lúc.
- Đồng nhất "image nhỏ" với "image an toàn" mà không có phân tích bổ sung.

Lưu ý quan trọng:

**Image nhỏ hơn thường giúp giảm attack surface, nhưng không tự động đồng nghĩa với an toàn tuyệt đối.**

## 15. Hướng làm thực tế cho nhóm trong 4 tuần

### Tuần 1

- Chốt tên đề tài.
- Chốt câu hỏi nghiên cứu, mục tiêu, phạm vi.
- Phân tích hiện trạng `Dockerfile` và `docker-compose.yml`.

### Tuần 2

- Refactor `GymBro.API/Dockerfile`.
- Chuẩn hóa lại `GymBro.Web/Dockerfile`.
- Tách cấu hình dev/prod và xử lý secrets tốt hơn.

### Tuần 3

- Thực hiện build, scan, đo đạc.
- Chụp số liệu trước và sau cải tiến.
- Kiểm tra ứng dụng còn chạy ổn định hay không.

### Tuần 4

- Viết báo cáo.
- Làm slide.
- Chuẩn bị demo so sánh mô hình cơ sở và mô hình cải tiến.

## 16. Gợi ý phân công cho nhóm

Nếu nhóm có 3 người, có thể chia như sau:

### Thành viên 1

- Phụ trách cơ sở lý thuyết
- Viết phần multistage build, image layers, attack surface

### Thành viên 2

- Phụ trách triển khai kỹ thuật
- Refactor Dockerfile, Compose, runtime hardening

### Thành viên 3

- Phụ trách thực nghiệm và đánh giá
- Đo image size, quét lỗ hổng, lập bảng so sánh, viết kết quả

## 17. Chốt định hướng ngắn gọn để nhóm đỡ hoang mang

Nhóm có thể nhớ một câu duy nhất:

**Đề tài của nhóm không phải là "học cách viết multistage Dockerfile", mà là "dùng multistage làm nền để xây dựng image an toàn hơn và chứng minh điều đó bằng số liệu".**

Nếu cần nói ngắn với thầy, nhóm có thể trình bày như sau:

**Nhóm chọn hướng nghiên cứu áp dụng Docker multistage kết hợp hardening container để giảm bề mặt tấn công, giảm rủi ro cấu hình không an toàn và nâng cao mức độ bảo mật khi triển khai ứng dụng .NET bằng Docker.**

## 18. Kết luận

Hướng thầy định hướng là đúng và tốt cho nhóm, vì:

- Vẫn giữ được phần cốt lõi là **multistage build**.
- Mở rộng được sang **bảo mật**, nên đề tài đủ sâu hơn.
- Có thể đo đạc, so sánh, đánh giá nên phù hợp với tinh thần nghiên cứu khoa học.
- Bám được trực tiếp vào dự án hiện tại của nhóm, không bị lý thuyết suông.

Kết luận cuối cùng:

**Nhóm nên chốt đề tài theo hướng "multistage để tăng cường bảo mật container", lấy dự án hiện tại làm đối tượng thực nghiệm, và đánh giá bằng số liệu trước/sau cải tiến.**
