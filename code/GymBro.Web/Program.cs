using GymBro.Infrastructure;
using GymBro.Application;
using GymBro.Web.Features.AdminPayments;
using GymBro.Web.Features.AdminReviews;

var builder = WebApplication.CreateBuilder(args);

// =========================================================
// 1. ĐĂNG KÝ DỊCH VỤ (SERVICES)
// =========================================================

builder.Services.AddControllersWithViews();
builder.Services.AddApplicationServices();

// Đăng ký hạ tầng
builder.Services.AddInfrastructureServices(builder.Configuration);

// Cấu hình Session (Dùng cho Đăng nhập và Giỏ hàng)
builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Session hết hạn sau 30 phút
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Đăng ký HttpContextAccessor để truy cập Session từ các Class Helper (nếu có)
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await AdminPaymentsDemoSeed.SeedAsync(app.Services, app.Logger);
    await AdminReviewsDemoSeed.SeedAsync(app.Services, app.Logger);
}

// =========================================================
// 2. CẤU HÌNH PIPELINE (MIDDLEWARE)
// =========================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// QUAN TRỌNG: Thứ tự middleware - UseSession PHẢI nằm trước UseAuthorization
app.UseSession();

app.UseAuthorization();

// =========================================================
// 3. CẤU HÌNH ĐƯỜNG DẪN (ROUTING)
// =========================================================

// SỬA LỖI 404: Route cho vùng quản trị Admin (Area)
// Thiết lập mặc định controller=Admin và action=Dashboard để khớp với AdminController.cs của bạn


// Route mặc định cho khách hàng
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
