using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymBro.Core;

/// <summary>
/// Khớp bảng Users trong DB: Username, Password (BCrypt hash), Email, Role, ...
/// Không kế thừa IdentityUser — auth dùng BCrypt trực tiếp trên cột Password.
/// </summary>
[Table("Users")]
public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Password { get; set; } = string.Empty;

    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [StringLength(200)]
    public string Address { get; set; } = string.Empty;

    [StringLength(20)]
    public string Role { get; set; } = "User";

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    public virtual ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
}
