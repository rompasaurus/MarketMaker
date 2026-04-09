FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY src/PolymarketTracker.Api/PolymarketTracker.Api.csproj src/PolymarketTracker.Api/
RUN dotnet restore src/PolymarketTracker.Api/PolymarketTracker.Api.csproj

COPY . .
RUN dotnet publish src/PolymarketTracker.Api/PolymarketTracker.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app

RUN apk add --no-cache curl

COPY --from=build /app/publish .

RUN mkdir -p /app/logs

EXPOSE 5000

ENTRYPOINT ["dotnet", "PolymarketTracker.Api.dll"]
