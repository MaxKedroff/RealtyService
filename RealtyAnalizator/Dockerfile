FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["RealtyAnalizator/RealtyAnalizator.csproj", "RealtyAnalizator/"]
COPY ["Application/Application.csproj", "Application/"]
COPY ["Infrastructure/Infrastructure.csproj", "Infrastructure/"]
COPY ["Core/Core.csproj", "Core/"]
COPY ["ParsingService/ParsingService.csproj", "ParsingService/"]

RUN dotnet restore "RealtyAnalizator/RealtyAnalizator.csproj"

COPY . .

WORKDIR /src/RealtyAnalizator
RUN dotnet build "RealtyAnalizator.csproj" -c Release --no-restore

FROM build AS publish
RUN dotnet publish "RealtyAnalizator.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Development
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

EXPOSE 8080

ENTRYPOINT ["dotnet", "RealtyAnalizator.dll"]