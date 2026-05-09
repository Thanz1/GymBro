# ==========================================
# GIAI ĐOẠN 1: Build mã nguồn (Sử dụng SDK nặng)
# ==========================================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Sao chép các file .csproj để thực hiện restore dependencies (tối ưu cache)
COPY ["GymBro.Web/GymBro.Web.csproj", "GymBro.Web/"]
COPY ["GymBro.API/GymBro.API.csproj", "GymBro.API/"]
COPY ["GymBro.Core/GymBro.Core.csproj", "GymBro.Core/"]
COPY ["GymBro.Infrastructure/GymBro.Infrastructure.csproj", "GymBro.Infrastructure/"]

# Khôi phục các gói thư viện NuGet
RUN dotnet restore "GymBro.Web/GymBro.Web.csproj"

# Sao chép toàn bộ mã nguồn còn lại vào container
COPY . .
WORKDIR "/src/GymBro.Web"

# Biên dịch ứng dụng sang chế độ Release
RUN dotnet build "GymBro.Web.csproj" -c Release -o /app/build

# ==========================================
# GIAI ĐOẠN 2: Xuất bản ứng dụng (Publish)
# ==========================================
FROM build AS publish
RUN dotnet publish "GymBro.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ==========================================
# GIAI ĐOẠN 3: Môi trường vận hành (Runtime - Tối ưu bảo mật)
# ==========================================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Sao chép kết quả đã build từ giai đoạn Publish sang (Chỉ lấy file thực thi)
COPY --from=publish /app/publish .

# --- BƯỚC BẢO MẬT (HARDENING) ---
# 1. Tạo một người dùng mới không có quyền quản trị (non-root)
RUN adduser --disabled-password --gecos "" gymuser

# 2. Phân quyền cho người dùng mới sở hữu thư mục ứng dụng
RUN chown -R gymuser /app

# 3. Chuyển sang sử dụng người dùng này để chạy ứng dụng
USER gymuser
# --------------------------------

# Thiết lập lệnh khởi chạy ứng dụng
ENTRYPOINT ["dotnet", "GymBro.Web.dll"]