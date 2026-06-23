using GymBro.Infrastructure;
using GymBro.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// =========================================================
// 1. ĐĂNG KÝ DỊCH VỤ (SERVICES)
// =========================================================

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- Đăng ký DbContext dùng chung (Chuẩn SOA) ---
builder.Services.AddDbContext<ProductDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
    // Lưu ý: Migrations sẽ được lưu tại dự án API này
    x => x.MigrationsAssembly("GymBro.Infrastructure")));

// --- Đăng ký Service & Repository ---
// Đăng ký Service để xử lý logic (Đã dọn sạch lỗi cú pháp, KHÔNG dùng HttpClient ở đây)
//builder.Services.AddScoped<IProductService, ProductService>();

// Đăng ký Repository để làm việc với Database thông qua GymBroDbContext
builder.Services.AddScoped<IProductRepository, ProductRepository>();

// --- Đăng ký AutoMapper ---
// Chuẩn .NET 8: Tự động quét các cấu hình (Profile) trong dự án hiện tại
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddMaps(typeof(Program).Assembly);
});

var app = builder.Build();

// =========================================================
// 2. TỰ ĐỘNG MIGRATE DATABASE (CÓ RETRY KHI SQL CHƯA SẴN SÀNG)
// =========================================================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    var maxRetries = 5;
    var delay = TimeSpan.FromSeconds(5);
    
    for (int i = 1; i <= maxRetries; i++)
    {
        try
        {
            db.Database.Migrate();
            logger.LogInformation("[GymBro.Product.API] Migration thành công.");
            break;
        }
        catch (Exception ex) when (i < maxRetries)
        {
            logger.LogWarning("[GymBro.Product.API] Lần {Attempt}/{MaxRetries} - Migration thất bại: {Message}. Thử lại sau {Delay}s...", 
                i, maxRetries, ex.Message, delay.TotalSeconds);
            Thread.Sleep(delay);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[GymBro.Product.API] Migration thất bại sau {MaxRetries} lần thử.", maxRetries);
        }
    }
}

// =========================================================
// 3. CẤU HÌNH PIPELINE (MIDDLEWARE)
// =========================================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Chèn Authentication trước Authorization nếu có dùng đăng nhập
app.UseAuthorization();

app.MapControllers();

app.Run();