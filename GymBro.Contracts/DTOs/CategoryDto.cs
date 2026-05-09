using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymBro.Contracts.DTOs
{
    public class CategoryDto
    {
        public int Id { get; set; } // Kiểu int để đồng bộ với controller
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
