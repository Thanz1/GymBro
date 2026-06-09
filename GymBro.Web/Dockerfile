FROM mcr.microsoft.com/dotnet/sdk:8.0
WORKDIR /app

COPY . .

WORKDIR "/app/GymBro.Web"
RUN dotnet restore "GymBro.Web.csproj"
RUN dotnet publish "GymBro.Web.csproj" -c Release -o /app/publish /p:ErrorOnDuplicatePublishOutputFiles=false

WORKDIR /app/publish
EXPOSE 8080
ENTRYPOINT ["dotnet", "GymBro.Web.dll"]
