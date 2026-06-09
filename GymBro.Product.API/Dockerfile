# Sử dụng Image SDK chứa toàn bộ bộ công cụ biên dịch
FROM mcr.microsoft.com/dotnet/sdk:8.0
WORKDIR /app

# Copy toàn bộ Solution vào container
COPY . .

# Di chuyển vào đúng thư mục Product API để thao tác
WORKDIR "/app/GymBro.Product.API"

# Phục hồi Nuget và tiến hành Build trực tiếp
RUN dotnet restore "GymBro.Product.API.csproj"
RUN dotnet publish "GymBro.Product.API.csproj" -c Release -o /app/publish /p:ErrorOnDuplicatePublishOutputFiles=false
# Thiết lập thư mục chạy và file khởi động
WORKDIR /app/publish
EXPOSE 8080
ENTRYPOINT ["dotnet", "GymBro.Product.API.dll"]