FROM mcr.microsoft.com/dotnet/sdk:9.0 AS restore
WORKDIR /src

COPY ["GymBro.Web/GymBro.Web.csproj", "GymBro.Web/"]
COPY ["GymBro.Application/GymBro.Application.csproj", "GymBro.Application/"]
COPY ["GymBro.Core/GymBro.Core.csproj", "GymBro.Core/"]
COPY ["GymBro.Infrastructure/GymBro.Infrastructure.csproj", "GymBro.Infrastructure/"]
COPY ["GymBro.Tests/GymBro.Tests.csproj", "GymBro.Tests/"]

RUN dotnet restore "GymBro.Web/GymBro.Web.csproj"
RUN dotnet restore "GymBro.Tests/GymBro.Tests.csproj"

FROM restore AS build
COPY . .
WORKDIR /src/GymBro.Web
RUN dotnet build "GymBro.Web.csproj" -c Release --no-restore -o /app/build

FROM build AS test
WORKDIR /src
RUN dotnet test "GymBro.Tests/GymBro.Tests.csproj" -c Release --no-restore

FROM test AS migration-bundle
WORKDIR /src
RUN dotnet tool install --tool-path /opt/dotnet-tools dotnet-ef --version 8.0.0
RUN /opt/dotnet-tools/dotnet-ef migrations bundle \
    --project "GymBro.Infrastructure/GymBro.Infrastructure.csproj" \
    --startup-project "GymBro.Web/GymBro.Web.csproj" \
    --configuration Release \
    --output /app/migrator/gymbro-migrate \
    --force

FROM test AS publish
WORKDIR /src/GymBro.Web
RUN dotnet publish "GymBro.Web.csproj" -c Release --no-restore -o /app/publish /p:UseAppHost=false /p:DebugType=None /p:DebugSymbols=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=publish /app/publish .

RUN rm -f ./*.pdb ./appsettings.Development.json

RUN adduser --disabled-password --gecos "" gymuser \
    && mkdir -p /app/wwwroot/Content/Images /home/gymuser/.aspnet/DataProtection-Keys \
    && chown -R gymuser:gymuser /app /home/gymuser

USER gymuser

ENTRYPOINT ["dotnet", "GymBro.Web.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS migrator
WORKDIR /app
ENV DOTNET_BUNDLE_EXTRACT_BASE_DIR=/tmp/dotnet-bundle

COPY --from=migration-bundle /app/migrator/gymbro-migrate .

RUN adduser --disabled-password --gecos "" gymmigrator \
    && chmod +x /app/gymbro-migrate \
    && chown -R gymmigrator:gymmigrator /app

USER gymmigrator

ENTRYPOINT ["/app/gymbro-migrate"]
