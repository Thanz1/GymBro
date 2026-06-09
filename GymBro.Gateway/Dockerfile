FROM mcr.microsoft.com/dotnet/sdk:8.0
WORKDIR /app

COPY . .

WORKDIR "/app/GymBro.Gateway"
RUN dotnet restore "GymBro.Gateway.csproj"
RUN dotnet publish "GymBro.Gateway.csproj" -c Release -o /app/publish /p:ErrorOnDuplicatePublishOutputFiles=false

WORKDIR /app/publish
EXPOSE 8080
ENTRYPOINT ["dotnet", "GymBro.Gateway.dll"]