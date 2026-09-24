# Multi-stage build for HFL Enterprise Server (.NET 9)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

COPY HFL_Razbloker.sln .
COPY src/HFL.Core/ src/HFL.Core/
COPY src/HFL.Server/ src/HFL.Server/

RUN dotnet restore src/HFL.Server/HFL.Server.csproj
RUN dotnet publish src/HFL.Server/HFL.Server.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Expose HTTP (DoH / API) and DNS (Port 53 UDP)
EXPOSE 80
EXPOSE 443
EXPOSE 53/udp
EXPOSE 53/tcp

ENV ASPNETCORE_URLS=http://+:80
ENV DB_PATH=/app/data/hfl_enterprise.db

ENTRYPOINT ["dotnet", "HFL.Server.dll"]
