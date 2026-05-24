namespace GymBro.Contracts;
    // Dữ liệu người dùng gửi lên khi Đăng Ký
    public class RegisterDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    // Dữ liệu người dùng gửi lên khi Đăng Nhập
    public class LoginDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    // ID token từ Google Identity Services (đăng nhập Gmail)
    public class GoogleLoginDto
    {
        public string IdToken { get; set; } = string.Empty;
    }

 
    

    // Dữ liệu danh mục
    public class CategoryDto
    {
        public int Id { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    
