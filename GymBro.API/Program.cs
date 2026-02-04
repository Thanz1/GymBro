using GymBro.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer; // <-- Cần cái này
using Microsoft.IdentityModel.Tokens;              // <-- Cần cái này
using System.Text;                                 // <-- Cần cái này

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------
// A. ĐĂNG KÝ DỊCH VỤ (SERVICES)
// ---------------------------------------------------------

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// --- 1. CẤU HÌNH SWAGGER (Để hiện nút khóa) ---
builder.Services.AddSwaggerGen(option =>
{
    option.SwaggerDoc("v1", new OpenApiInfo { Title = "GymBro API", Version = "v1" });
    option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Vui lòng nhập Token vào ô bên dưới (Ví dụ: Bearer eyJ...)",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });
    option.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type=ReferenceType.SecurityScheme,
                    Id="Bearer"
                }
            },
            new string[]{}
        }
    });
});

// --- 2. CẤU HÌNH JWT (QUAN TRỌNG - BỊ THIẾU LÚC NÃY) ---
// Đoạn này dạy hệ thống cách đọc Token
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            builder.Configuration.GetSection("Jwt:Key").Value!)), // Lấy chìa khóa từ appsettings
        ValidateIssuer = false,  // Bỏ qua check Issuer (để dễ test)
        ValidateAudience = false // Bỏ qua check Audience (để dễ test)
    };
});
// -------------------------------------------------------

builder.Services.AddDbContext<GymBroDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// ---------------------------------------------------------
// B. CẤU HÌNH PIPELINE (MIDDLEWARE)
// ---------------------------------------------------------

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Thứ tự 2 dòng này cực kỳ quan trọng:
app.UseAuthentication(); // 1. Kiểm tra vé (Bạn là ai?)
app.UseAuthorization();  // 2. Kiểm tra quyền (Bạn được vào không?)

app.MapControllers();

app.Run();