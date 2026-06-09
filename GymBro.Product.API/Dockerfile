# STAGE 1: MÔI TRƯỜNG BIÊN DỊCH (SDK nặng)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy toàn bộ solution để nhận đủ các project phụ thuộc (Core, Contracts...)
COPY . .
WORKDIR "/src/GymBro.Product.API"

# Restore và Publish (có cờ chống lỗi trùng file)
RUN dotnet restore "GymBro.Product.API.csproj"
RUN dotnet publish "GymBro.Product.API.csproj" -c Release -o /app/publish /p:UseAppHost=false /p:ErrorOnDuplicatePublishOutputFiles=false

# STAGE 2: MÔI TRƯỜNG THỰC THI (ASPNET siêu nhẹ)
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
EXPOSE 8080
# Chỉ lấy thành phẩm từ STAGE 1 sang
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "GymBro.Product.API.dll"]