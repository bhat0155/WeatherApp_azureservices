# Stage1 build

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY WeatherApp.sln .
COPY WeatherApp.Api/WeatherApp.Api.csproj WeatherApp.Api/
COPY WeatherApp.Tests/WeatherApp.Tests.csproj WeatherApp.Tests/

RUN dotnet restore WeatherApp.sln

COPY . .

RUN dotnet publish WeatherApp.Api/WeatherApp.Api.csproj \
      --no-restore \
      --configuration Release \
      --output /app/publish

# Stage2 runtime

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "WeatherApp.Api.dll"]