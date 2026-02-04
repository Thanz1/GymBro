using GymBro.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. ĐĂNG KÝ DỊCH VỤ
builder.Services.AddControllersWithViews();

// Đăng ký Database
builder.Services.AddDbContext<GymBroDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Đăng ký Session (Lưu giỏ hàng, đăng nhập)
builder.Services.AddSession(options => { // <--- MỚI THÊM
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Đăng ký HttpContextAccessor (Để truy cập Session từ Helper)
builder.Services.AddHttpContextAccessor(); // <--- MỚI THÊM

var app = builder.Build();

// 2. CẤU HÌNH PIPELINE
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession(); // <--- QUAN TRỌNG: Phải đặt trước UseAuthorization

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();