FROM mcr.microsoft.com/dotnet/sdk:8.0
WORKDIR /app

COPY . .

WORKDIR "/app/GymBro.Order.API"
RUN dotnet restore "GymBro.Order.API.csproj"
RUN dotnet publish "GymBro.Order.API.csproj" -c Release -o /app/publish /p:ErrorOnDuplicatePublishOutputFiles=false

WORKDIR /app/publish
EXPOSE 8080
ENTRYPOINT ["dotnet", "GymBro.Order.API.dll"]