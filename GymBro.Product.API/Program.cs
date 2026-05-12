using GymBro.Infrastructure; // SỬA: Bỏ .Repositories
using GymBro.Service;        // SỬA: Bỏ .Services
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// =========================================================
// 1. ĐĂNG KÝ DỊCH VỤ (SERVICES)
// =========================================================

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- Đăng ký DbContext dùng chung (Chuẩn SOA) ---
builder.Services.AddDbContext<GymBroDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
    // Lưu ý: Migrations sẽ được lưu tại dự án API này
    x => x.MigrationsAssembly("GymBro.Product.API")));

// --- Đăng ký HttpClient & Repository ---
// Đăng ký ProductService theo dạng HttpClient nếu Service này gọi API khác hoặc dùng Typed Client
builder.Services.AddHttpClient<IProductService, ProductService>(client =>
{
    client.BaseAddress = new Uri("https://localhost:7002/");
});

// Đăng ký Repository để làm việc với Database thông qua GymBroDbContext
builder.Services.AddScoped<IProductRepository, ProductRepository>();

// --- Đăng ký AutoMapper ---
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

var app = builder.Build();

// =========================================================
// 2. CẤU HÌNH PIPELINE (MIDDLEWARE)
// =========================================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Chèn Authentication trước Authorization nếu Thành có dùng đăng nhập
app.UseAuthorization();

app.MapControllers();

app.Run();