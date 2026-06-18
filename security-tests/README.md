# Security Tests cho Docker Multi-stage GymBro

Thư mục này chứa bộ kiểm tra mô phỏng tấn công an toàn cho Docker image/container của GymBro. Mục tiêu là tạo bằng chứng cho đề tài nghiên cứu: secure multi-stage build giúp giảm SDK/source code trong runtime image, giảm quyền chạy, giảm rò rỉ secrets và hỗ trợ hardening khi deploy.

## File trong thư mục

| File | Vai trò |
| --- | --- |
| `run-docker-attack-tests.ps1` | Script chạy kiểm tra image, container và Docker Compose |
| `docker-attack-targets.json` | Danh sách image/container cần kiểm tra |
| `attack-cases.md` | Mô tả từng kịch bản attack/security test |
| `reports/` | Thư mục report tự sinh sau khi chạy script |

## Cách chạy nhanh

Chạy kiểm tra tất cả image có trong `docker-attack-targets.json`:

```powershell
powershell -ExecutionPolicy Bypass -File .\security-tests\run-docker-attack-tests.ps1 -Profile all
```

Chỉ kiểm tra các image theo `docker-compose.yml` ở root:

```powershell
powershell -ExecutionPolicy Bypass -File .\security-tests\run-docker-attack-tests.ps1 -Profile compose
```

Chỉ kiểm tra các image nghiên cứu/báo cáo đã build trước đó như `gymbro:single-stage`, `gymbro/web:secure-multistage`, `gymbro/api:secure-multistage`:

```powershell
powershell -ExecutionPolicy Bypass -File .\security-tests\run-docker-attack-tests.ps1 -Profile research
```

Build compose trước khi test:

```powershell
powershell -ExecutionPolicy Bypass -File .\security-tests\run-docker-attack-tests.ps1 -Profile compose -BuildCompose
```

Khởi động compose trước khi test runtime container:

```powershell
powershell -ExecutionPolicy Bypass -File .\security-tests\run-docker-attack-tests.ps1 -Profile compose -StartCompose
```

Chạy thêm quét lỗ hổng nếu máy có Trivy hoặc Docker Scout:

```powershell
powershell -ExecutionPolicy Bypass -File .\security-tests\run-docker-attack-tests.ps1 -Profile all -RunScanner
```

## Kết quả

Sau khi chạy, script sẽ in bảng kết quả ra terminal và tạo report Markdown trong:

```text
security-tests/reports/
```

Các trạng thái:

- `PASS`: đạt yêu cầu.
- `FAIL`: phát hiện rủi ro rõ.
- `WARN`: có rủi ro trong production, nhưng có thể chấp nhận ở môi trường demo/dev.
- `SKIP`: thiếu image/container/công cụ nên không kiểm tra được.
- `INFO`: thông tin hỗ trợ đánh giá.

## Gợi ý đưa vào báo cáo

Có thể dùng report sinh ra để bổ sung chương thực nghiệm:

- So sánh `gymbro:single-stage` với `gymbro/web:secure-multistage`.
- Chụp lại các test `dotnet --list-sdks`, kiểm tra source code và kiểm tra user runtime.
- Nếu baseline có `FAIL` còn secure multi-stage có `PASS`, đó là bằng chứng cho thấy mô hình đề xuất giảm bề mặt tấn công.

Lưu ý: `docker-compose.yml` hiện tại của root vẫn dùng `ASPNETCORE_ENVIRONMENT=Development` và có mật khẩu SQL Server/RabbitMQ trực tiếp trong file compose. Vì vậy các test Compose có thể báo `FAIL` hoặc `WARN`. Điều này phản ánh đúng hiện trạng và là cơ sở để đề xuất cải tiến sang `.env`, secrets, `Production`, non-root user và hardening runtime.
