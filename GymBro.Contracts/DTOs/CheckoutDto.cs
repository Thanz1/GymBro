using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymBro.Contracts
{
    public class CheckoutDto
    {
        public int UserId { get; set; }

        public List<CartItemDto> CartItems { get; set; } = new();
        public int PaymentMethodId { get; set; }

        // THÊM DÒNG NÀY: Để hết lỗi CS0117
        public DateTime OrderDate { get; set; }
    }
}
