using GymBro.Core;
using GymBro.Identity.API.Email;
using GymBro.Identity.API.Hubs;
using GymBro.Identity.API.Messaging;
using GymBro.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- THÊM CẤU HÌNH CORS CHO SIGNALR ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("ChatCorsPolicy", policyBuilder =>
    {
        policyBuilder.SetIsOriginAllowed(_ => true) // Chấp nhận mọi nguồn gọi tới (để dễ test)
                     .AllowAnyMethod()
                     .AllowAnyHeader()
                     .AllowCredentials(); // Bắt buộc phải có để SignalR hoạt động
    });
});
// --------------------------------------

builder.Services.Configure<RabbitMqOptions>(
    builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.Configure<SmtpOptions>(
    builder.Configuration.GetSection(SmtpOptions.SectionName));
builder.Services.AddSingleton<IIntegrationEventPublisher, RabbitMqIntegrationEventPublisher>();
builder.Services.AddScoped<IWelcomeEmailSender, SmtpWelcomeEmailSender>();
builder.Services.AddHostedService<UserCreatedWelcomeEmailConsumer>();
builder.Services.AddSignalR();
builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.MigrationsAssembly("GymBro.Infrastructure")));

var app = builder.Build();

try
{
    _ = app.Services.GetRequiredService<IIntegrationEventPublisher>();
    Console.WriteLine("[GYMBRO Identity] RabbitMQ exchange 'gymbro.events' đã sẵn sàng.");
}
catch (Exception ex)
{
    Console.WriteLine($"[GYMBRO Identity] RabbitMQ chưa kết nối được: {ex.Message}");
}

// ========================================
// TỰ ĐỘNG MIGRATE DATABASE (CÓ RETRY)
// ========================================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    var maxRetries = 5;
    var delay = TimeSpan.FromSeconds(5);
    
    for (int i = 1; i <= maxRetries; i++)
    {
        try
        {
            db.Database.Migrate();
            logger.LogInformation("[GYMBRO Identity] Migration thành công.");
            break;
        }
        catch (Exception ex) when (i < maxRetries)
        {
            logger.LogWarning("[GYMBRO Identity] Lần {Attempt}/{MaxRetries} - Migration thất bại: {Message}. Thử lại sau {Delay}s...", 
                i, maxRetries, ex.Message, delay.TotalSeconds);
            Thread.Sleep(delay);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[GYMBRO Identity] Migration thất bại sau {MaxRetries} lần thử.", maxRetries);
        }
    }

    // ========================================
    // SEED TÀI KHOẢN MẶC ĐỊNH
    // ========================================
    if (!db.Users.Any())
    {
        logger.LogInformation("[GYMBRO Identity] Đang seed tài khoản mặc định...");

        db.Users.AddRange(
            new User
            {
                Username = "admin",
                Password = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                Email = "admin@gymbro.com",
                FullName = "Quản trị viên",
                Address = "TP. Hồ Chí Minh",
                Role = "Admin",
                IsActive = true,
                CreatedDate = DateTime.Now
            },
            new User
            {
                Username = "user",
                Password = BCrypt.Net.BCrypt.HashPassword("User@123"),
                Email = "user@gymbro.com",
                FullName = "Người dùng",
                Address = "TP. Hồ Chí Minh",
                Role = "User",
                IsActive = true,
                CreatedDate = DateTime.Now
            },
            new User
            {
                Username = "staff",
                Password = BCrypt.Net.BCrypt.HashPassword("Staff@123"),
                Email = "staff@gymbro.com",
                FullName = "Nhân viên",
                Address = "TP. Hồ Chí Minh",
                Role = "Staff",
                IsActive = true,
                CreatedDate = DateTime.Now
            }
        );
        db.SaveChanges();
        logger.LogInformation("[GYMBRO Identity] Đã seed 3 tài khoản: admin, user, staff");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
// --- KÍCH HOẠT CORS TRƯỚC KHI ỦY QUYỀN VÀ MAP HUB ---
app.UseCors("ChatCorsPolicy");

app.UseAuthorization();
app.MapHub<ChatHub>("/chatHub");
app.MapControllers();
app.Run();