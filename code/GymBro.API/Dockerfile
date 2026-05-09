FROM mcr.microsoft.com/dotnet/sdk:9.0 AS restore
WORKDIR /src

COPY ["GymBro.API/GymBro.API.csproj", "GymBro.API/"]
COPY ["GymBro.Application/GymBro.Application.csproj", "GymBro.Application/"]
COPY ["GymBro.Core/GymBro.Core.csproj", "GymBro.Core/"]
COPY ["GymBro.Infrastructure/GymBro.Infrastructure.csproj", "GymBro.Infrastructure/"]
COPY ["GymBro.Tests/GymBro.Tests.csproj", "GymBro.Tests/"]

RUN dotnet restore "GymBro.API/GymBro.API.csproj"
RUN dotnet restore "GymBro.Tests/GymBro.Tests.csproj"

FROM restore AS build
COPY . .
WORKDIR /src/GymBro.API
RUN dotnet build "GymBro.API.csproj" -c Release --no-restore -o /app/build

FROM build AS test
WORKDIR /src
RUN dotnet test "GymBro.Tests/GymBro.Tests.csproj" -c Release --no-restore

FROM test AS publish
WORKDIR /src/GymBro.API
RUN dotnet publish "GymBro.API.csproj" -c Release --no-restore -o /app/publish /p:UseAppHost=false /p:DebugType=None /p:DebugSymbols=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=publish /app/publish .

RUN rm -f ./*.pdb ./appsettings.Development.json

RUN adduser --disabled-password --gecos "" gymapi \
    && mkdir -p /home/gymapi/.aspnet/DataProtection-Keys \
    && chown -R gymapi:gymapi /app /home/gymapi

USER gymapi

ENTRYPOINT ["dotnet", "GymBro.API.dll"]
