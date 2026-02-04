namespace GymBro.API.DTOs
{
    public class CreateProductDto
    {
        public string TenSanPham { get; set; } = string.Empty;
        public decimal Gia { get; set; }
        public string MoTa { get; set; } = string.Empty;
        public string HinhAnhUrl { get; set; } = string.Empty;
        public string DanhMuc { get; set; } = string.Empty;
        public int SoLuongTon { get; set; }
    }
}