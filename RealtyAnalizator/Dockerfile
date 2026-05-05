FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY *.sln ./

COPY RealtyAnalizator/*.csproj ./RealtyAnalizator/
COPY Application/*.csproj ./Application/
COPY Infrastructure/*.csproj ./Infrastructure/
COPY Core/*.csproj ./Core/
COPY ParsingService/*.csproj ./ParsingService/

RUN dotnet restore ./RealtyAnalizator/RealtyAnalizator.csproj

COPY . .

RUN dotnet build ./RealtyAnalizator/RealtyAnalizator.csproj -c Release --no-restore

RUN dotnet publish ./RealtyAnalizator/RealtyAnalizator.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Development

EXPOSE 8080

ENTRYPOINT ["dotnet", "RealtyAnalizator.dll"]