using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymBro.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Đảm bảo tất cả tài khoản hiện tại không bị khóa sau khi thêm cột IsActive
            migrationBuilder.Sql("UPDATE Users SET IsActive = 1 WHERE IsActive = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Khôi phục dữ liệu về trạng thái ban đầu (tùy chọn)
            migrationBuilder.Sql("UPDATE Users SET IsActive = 0 WHERE IsActive = 1");
        }
    }
}
