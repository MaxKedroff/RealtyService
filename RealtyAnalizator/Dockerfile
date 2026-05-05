FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["*.sln", "."]
COPY ["RealtyAnalizator/*.csproj", "API/"]
COPY ["Application/*.csproj", "Application/"]
COPY ["Infrastructure/*.csproj", "Infrastructure/"]
COPY ["Core/*.csproj", "Core/"]
COPY ["ParsingService/*.csproj", "ParsingService/"]

RUN dotnet restore

COPY ["*.sln", "."]
COPY ["RealtyAnalizator/*.csproj", "API/"]
COPY ["Application/*.csproj", "Application/"]
COPY ["Infrastructure/*.csproj", "Infrastructure/"]
COPY ["Core/*.csproj", "Core/"]
COPY ["ParsingService/*.csproj", "ParsingService/"]


RUN dotnet build -c Release --no-restore

FROM build AS publish
RUN dotnet publish "API/API.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV LD_LIBRARY_PATH=/usr/lib/x86_64-linux-gnu

ENTRYPOINT ["dotnet", "API.dll"]