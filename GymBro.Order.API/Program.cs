using GymBro.Core;
using GymBro.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();



builder.Services.AddHttpClient("ProductService", client =>
{
    var productApi = builder.Configuration["ServiceUrls:ProductApi"] ?? "https://localhost:7002";
    client.BaseAddress = new Uri(productApi.TrimEnd('/') + "/");
});

builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.MigrationsAssembly("GymBro.Order.API")));

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is required in appsettings.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// ========================================
// TỰ ĐỘNG MIGRATE DATABASE (CÓ RETRY)
// ========================================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    var maxRetries = 5;
    var delay = TimeSpan.FromSeconds(5);
    
    for (int i = 1; i <= maxRetries; i++)
    {
        try
        {
            db.Database.Migrate();
            logger.LogInformation("[GymBro.Order.API] Migration thành công.");
            break;
        }
        catch (Exception ex) when (i < maxRetries)
        {
            logger.LogWarning("[GymBro.Order.API] Lần {Attempt}/{MaxRetries} - Migration thất bại: {Message}. Thử lại sau {Delay}s...", 
                i, maxRetries, ex.Message, delay.TotalSeconds);
            Thread.Sleep(delay);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[GymBro.Order.API] Migration thất bại sau {MaxRetries} lần thử.", maxRetries);
        }
    }
    
    // Seed dữ liệu PaymentMethods nếu chưa có
    if (!await db.PaymentMethods.AnyAsync())
    {
        db.PaymentMethods.AddRange(
            new PaymentMethod
            {
                MethodName = "Thanh toán khi nhận hàng (COD)",
                Description = "Thanh toán tiền mặt khi nhận hàng",
                IsActive = true
            },
            new PaymentMethod
            {
                MethodName = "Chuyển khoản Ngân hàng (QR Code)",
                Description = "Quét mã QR để chuyển khoản",
                IsActive = true
            });
        await db.SaveChangesAsync();
        logger.LogInformation("[GymBro.Order.API] Đã seed dữ liệu PaymentMethods.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();