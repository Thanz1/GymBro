 using GymBro.Service;
using GymBro.Contracts;

var builder = WebApplication.CreateBuilder(args);

// =========================================================
// 1. ĐĂNG KÝ DỊCH VỤ (SERVICES)
// =========================================================

builder.Services.AddControllersWithViews();

// --- [SOA] Đăng ký HttpClient cho các Microservices ---

// Nhóm 1: Identity API (Port 7001) - Quản lý Tài khoản & Người dùng
builder.Services.AddHttpClient<IIdentityService, IdentityService>(client => {
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:IdentityApi"] ?? "http://localhost:5001");
});
builder.Services.AddHttpClient<IUserService, UserService>(client => {
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:IdentityApi"] ?? "http://localhost:5001");
});

// Nhóm 2: Product API (Port 7002) - Quản lý Sản phẩm, Danh mục, Đánh giá, NCC
builder.Services.AddHttpClient<IProductService, ProductService>(client => {
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:ProductApi"] ?? "https://localhost:7002");
});
builder.Services.AddHttpClient<ICategoryService, CategoryService>(client => {
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:ProductApi"] ?? "https://localhost:7002");
});
builder.Services.AddHttpClient<IReviewService, ReviewService>(client => {
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:ProductApi"] ?? "https://localhost:7002");
});
builder.Services.AddHttpClient<ISupplierService, SupplierService>(client => {
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:ProductApi"] ?? "https://localhost:7002");
});
// Nếu có InventoryService để chỉnh sửa kho:
// builder.Services.AddHttpClient<IInventoryService, InventoryService>(client => {
//    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:ProductApi"] ?? "https://localhost:7002");
// });

// Nhóm 3: Order API (Port 7003) - Quản lý Đơn hàng, Thanh toán, Wishlist
builder.Services.AddHttpClient<IOrderService, OrderService>(client => {
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:OrderApi"] ?? "https://localhost:7003");
});
builder.Services.AddHttpClient<IPaymentService, PaymentService>(client => {
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:OrderApi"] ?? "https://localhost:7003");
});
builder.Services.AddHttpClient<IWishlistService, WishlistService>(client => {
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:OrderApi"] ?? "https://localhost:7003");
});

// --- Cấu hình Session & Helper ---
builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();

var app = builder.Build();


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

// Cần UseSession trước UseAuthorization để lấy được User từ Session
app.UseSession();
app.UseAuthorization();

// Route cho Area Admin (Dành cho các Controller kế thừa BaseAdminController)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Admin}/{action=Dashboard}/{id?}");

// Route mặc định cho khách hàng
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();