namespace GymBro.API.DTOs
{
    public class AddToCartDto
    {
        public int ProductId { get; set; }
        public int SoLuong { get; set; }
    }

    public class CartItemDto
    {
        public string TenSanPham { get; set; } = string.Empty;
        public decimal DonGia { get; set; }
        public int SoLuong { get; set; }
        public decimal ThanhTien => DonGia * SoLuong;
    }
}