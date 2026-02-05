using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymBro.Core
{
    public static class OrderStatus
    {
        public const string Pending = "Chờ xử lý";
        public const string Confirmed = "Đã xác nhận";
        public const string Shipping = "Đang giao hàng";
        public const string Completed = "Đã giao hàng";
        public const string Cancelled = "Đã hủy";

        public static List<string> GetAllStatuses()
        {
            return new List<string>
            {
                Pending, Confirmed, Shipping, Completed, Cancelled
            };
        }
    }
}

