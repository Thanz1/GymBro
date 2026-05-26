using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Thêm file cấu hình ocelot.json vào hệ thống cấu hình của ứng dụng
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// Đăng ký các dịch vụ của Ocelot vào DI Container
builder.Services.AddOcelot();

var app = builder.Build();

// Kích hoạt Ocelot Middleware ở cuối quy trình xử lý request
await app.UseOcelot();

app.Run();