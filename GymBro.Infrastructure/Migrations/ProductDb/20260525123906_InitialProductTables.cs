using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymBro.Infrastructure.Migrations.ProductDb
{
    /// <inheritdoc />
    public partial class InitialProductTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ĐÃ XÓA TRẮNG - Đánh dấu đây là Baseline
            // Vì Database đã có sẵn các bảng của Product, không cần tạo lại.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ĐỂ TRỐNG
        }
    }
}