FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and restore as distinct layers
COPY GymBro.sln ./
COPY GymBro.API/GymBro.API.csproj GymBro.API/
COPY GymBro.Core/GymBro.Core.csproj GymBro.Core/
COPY GymBro.Infrastructure/GymBro.Infrastructure.csproj GymBro.Infrastructure/
COPY GymBro.Web/GymBro.Web.csproj GymBro.Web/

RUN dotnet restore GymBro.sln

# Copy the rest of the source and build
COPY . .
WORKDIR /src/GymBro.Web

RUN dotnet publish GymBro.Web.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Copy published output from build stage
COPY --from=build /app/publish .

# ASP.NET Core default port
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "GymBro.Web.dll"]

