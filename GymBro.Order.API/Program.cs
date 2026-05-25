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

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
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
