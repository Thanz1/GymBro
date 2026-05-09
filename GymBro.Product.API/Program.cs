using GymBro.Infrastructure; // SỬA: Bỏ .Repositories
using GymBro.Service;        // SỬA: Bỏ .Services
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Đăng ký DbContext
builder.Services.AddDbContext<GymBroDbContext>(options => // Đổi từ ProductDbContext thành GymBroDbContext
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
    x => x.MigrationsAssembly("GymBro.Product.API")));

// Đăng ký Service & Repository
// Lưu ý: Nếu ProductService của bạn cũng dùng HttpClient giống UserService, 
// thì hãy dùng AddHttpClient giống bên Identity nhé.
builder.Services.AddHttpClient<IProductService, ProductService>(client =>
{
    client.BaseAddress = new Uri("https://localhost:7002/"); // Đổi 7001 thành 7002
});
builder.Services.AddScoped<IProductRepository, ProductRepository>();

// Đăng ký AutoMapper
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();