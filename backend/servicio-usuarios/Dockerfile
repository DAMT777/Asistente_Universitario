# ---------- Etapa 1: compilar ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Se copian primero los .csproj para aprovechar la cache de Docker en el restore
COPY src/MiniIdentityApi.Domain/MiniIdentityApi.Domain.csproj src/MiniIdentityApi.Domain/
COPY src/MiniIdentityApi.Application/MiniIdentityApi.Application.csproj src/MiniIdentityApi.Application/
COPY src/MiniIdentityApi.Infrastructure/MiniIdentityApi.Infrastructure.csproj src/MiniIdentityApi.Infrastructure/
COPY src/MiniIdentityApi.Api/MiniIdentityApi.Api.csproj src/MiniIdentityApi.Api/
RUN dotnet restore src/MiniIdentityApi.Api/MiniIdentityApi.Api.csproj

COPY src/ src/
RUN dotnet publish src/MiniIdentityApi.Api/MiniIdentityApi.Api.csproj -c Release -o /out --no-restore

# ---------- Etapa 2: imagen final (solo runtime) ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "MiniIdentityApi.Api.dll"]
