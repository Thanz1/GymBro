2. Thiết lập Database trên Docker
Để không phải cài đặt SQL Server nặng nề, chúng ta sử dụng Docker. Chạy lệnh sau để tạo container Database với cấu hình khớp với code:

Bash

docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=GymBro@2024Password" -p 1433:1433 --name sql_gymbro -d mcr.microsoft.com/mssql/server:2022-latest
Lưu ý: Nếu bạn đã có SQL Server chạy ở cổng 1433, hãy đổi cổng hoặc dừng dịch vụ đó trước khi chạy lệnh trên.

3. Restore Packages & Build
Mở file GymBro.sln bằng Visual Studio.

Chuột phải vào Solution 'GymBro' -> Chọn Restore NuGet Packages.

Nhấn Ctrl + Shift + B để Build toàn bộ dự án.

4. Cập nhật cấu trúc Database (Migrations)
Để tạo các bảng dữ liệu (Users, Products, Orders...) vào Docker, bạn cần chạy Migration:

Mở Package Manager Console (Tools -> NuGet Package Manager).

Tại ô Default project, chọn: GymBro.Infrastructure.

Chạy lệnh:

PowerShell

Update-Database
5. Kích hoạt tài khoản Admin (Quan trọng)
Sau khi Update Database, tài khoản cũ có thể bị khóa mặc định do cột IsActive mới thêm. Hãy dùng SSMS hoặc công cụ quản lý DB chạy lệnh sau để mở khóa:

SQL

UPDATE Users SET IsActive = 1;
🛠 Cấu trúc dự án
GymBro.Core: Chứa các thực thể (Entities) và logic nghiệp vụ lõi.

GymBro.Infrastructure: Chứa DbContext, Migrations và cấu hình kết nối DB.

GymBro.Web: Giao diện người dùng MVC, Controllers và các thiết lập Web.

🔐 Tài khoản thử nghiệm
Admin: admin / 123456 (hoặc mật khẩu bạn đã tạo).
