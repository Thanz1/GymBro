using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymBro.Contracts
{
    public class WishlistDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ProductId { get; set; }

        // Flattening dữ liệu để hiển thị ở View
        public string? ProductName { get; set; }
        public string? ProductImage { get; set; }
        public decimal ProductPrice { get; set; }
        public string? ProductImageURL { get; set; }
        public int StockQuantity { get; set; }
        public DateTime CreatedDate { get; set; } 
    }
}
