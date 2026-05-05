FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Копируем .csproj файлы
COPY ["RealtyAnalizator/RealtyAnalizator.csproj", "RealtyAnalizator/"]
COPY ["Application/Application.csproj", "Application/"]
COPY ["Infrastructure/Infrastructure.csproj", "Infrastructure/"]
COPY ["Core/Core.csproj", "Core/"]
COPY ["ParsingService/ParsingService.csproj", "ParsingService/"]

# Restore
RUN dotnet restore "RealtyAnalizator/RealtyAnalizator.csproj"

COPY ["RealtyAnalizator/", "RealtyAnalizator/"]
COPY ["Application/", "Application/"]
COPY ["Infrastructure/", "Infrastructure/"]
COPY ["Core/", "Core/"]
COPY ["ParsingService/", "ParsingService/"]

# Build
WORKDIR /src/RealtyAnalizator
RUN dotnet build "RealtyAnalizator.csproj" -c Release --no-restore

# Publish
FROM build AS publish
RUN dotnet publish "RealtyAnalizator.csproj" -c Release -o /app/publish --no-restore

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Development

EXPOSE 8080

ENTRYPOINT ["dotnet", "RealtyAnalizator.dll"]