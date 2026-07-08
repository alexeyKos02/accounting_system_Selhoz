# --- Сборка ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Сначала только csproj — чтобы кешировать restore независимо от изменений кода.
COPY src/AgroInventory.Api/*.csproj                     src/AgroInventory.Api/
COPY src/AgroInventory.Application/*.csproj             src/AgroInventory.Application/
COPY src/AgroInventory.Domain/*.csproj                  src/AgroInventory.Domain/
COPY src/AgroInventory.Infrastructure/*.csproj          src/AgroInventory.Infrastructure/
RUN dotnet restore src/AgroInventory.Api/AgroInventory.Api.csproj

COPY src/ src/
RUN dotnet publish src/AgroInventory.Api/AgroInventory.Api.csproj -c Release -o /app --no-restore

# --- Рантайм ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# Railway пробрасывает порт через $PORT; локально — 8080.
EXPOSE 8080
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet AgroInventory.Api.dll"]
