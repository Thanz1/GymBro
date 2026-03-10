# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["GymBro.Web/GymBro.Web.csproj", "GymBro.Web/"]
COPY ["GymBro.Core/GymBro.Core.csproj", "GymBro.Core/"]
COPY ["GymBro.Infrastructure/GymBro.Infrastructure.csproj", "GymBro.Infrastructure/"]
RUN dotnet restore "GymBro.Web/GymBro.Web.csproj"

# Copy the remaining source code
COPY . .
WORKDIR "/src/GymBro.Web"
RUN dotnet build "GymBro.Web.csproj" -c Release -o /app/build

# Stage 2: Publish the application
FROM build AS publish
RUN dotnet publish "GymBro.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Run the application
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GymBro.Web.dll"]
