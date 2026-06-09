FROM mcr.microsoft.com/dotnet/sdk:8.0
WORKDIR /app

COPY . .

WORKDIR "/app/GymBro.Identity.API"
RUN dotnet restore "GymBro.Identity.API.csproj"
RUN dotnet publish "GymBro.Identity.API.csproj" -c Release -o /app/publish /p:ErrorOnDuplicatePublishOutputFiles=false

WORKDIR /app/publish
EXPOSE 8080
ENTRYPOINT ["dotnet", "GymBro.Identity.API.dll"]