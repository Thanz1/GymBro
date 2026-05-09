using GymBro.Core;
using GymBro.Infrastructure;
using GymBro.Service;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Cấu hình Database chính
builder.Services.AddDbContext<GymBroDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Cấu hình Database cho Identity
builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        x => x.MigrationsAssembly("GymBro.Identity.API")));

// Cấu hình Identity với kiểu dữ liệu int cho Role
builder.Services.AddIdentity<User, IdentityRole<int>>()
    .AddEntityFrameworkStores<IdentityDbContext>()
    .AddDefaultTokenProviders();

// Đăng ký Service xử lý User bằng AddHttpClient
builder.Services.AddHttpClient<IUserService, UserService>(client =>
{
    // Đảm bảo Port 7002 khớp với thiết lập thực tế của bạn
    client.BaseAddress = new Uri("https://localhost:7002/");
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// QUAN TRỌNG: Thứ tự Middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();