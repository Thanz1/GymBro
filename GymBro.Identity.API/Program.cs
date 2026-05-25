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

using (var scope = app.Services.CreateScope())
{
    // ĐÃ SỬA: Đổi từ GymBroDbContext sang IdentityDbContext
    var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    try
    {
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[GYMBRO Identity] Migration warning: {ex.Message}");
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