using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymBro.Order.API.Migrations
{
    /// <inheritdoc />
    public partial class Remove_FK_OrderDetail_Product : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Xóa FK OrderDetail -> Products
            migrationBuilder.DropForeignKey(
                name: "FK_OrderDetails_Products_ProductId",
                table: "OrderDetails");

            // Xóa FK InventoryTransactions -> Products
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_Products_ProductId",
                table: "InventoryTransactions");

            // Xóa FK PurchaseOrderDetails -> Products
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrderDetails_Products_ProductId",
                table: "PurchaseOrderDetails");

            // Xóa FK Products -> Categories (vì sắp xóa Products)
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products");

            // Xóa FK PurchaseOrders -> Suppliers
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_Suppliers_SupplierId",
                table: "PurchaseOrders");

            // Xóa bảng không thuộc về Order service:
            // Products, Categories, Suppliers thuộc về Product service
            // Order DB chỉ cần lưu ProductId dạng số nguyên, không FK

            // Xóa index trước khi xóa bảng
            migrationBuilder.DropIndex(
                name: "IX_OrderDetails_ProductId",
                table: "OrderDetails");

            migrationBuilder.DropIndex(
                name: "IX_InventoryTransactions_ProductId",
                table: "InventoryTransactions");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrderDetails_ProductId",
                table: "PurchaseOrderDetails");

            migrationBuilder.DropIndex(
                name: "IX_Products_CategoryId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_SupplierId",
                table: "PurchaseOrders");

            // Xóa các bảng không thuộc Order service
            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Suppliers");

            // Tạo lại index không có FK (chỉ là index thường để truy vấn nhanh)
            migrationBuilder.CreateIndex(
                name: "IX_OrderDetails_ProductId",
                table: "OrderDetails",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_ProductId",
                table: "InventoryTransactions",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderDetails_ProductId",
                table: "PurchaseOrderDetails",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Không hỗ trợ rollback - đây là migration tách microservices
        }
    }
}