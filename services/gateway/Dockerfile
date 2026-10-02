# Imagen del servicio de Gateway. Construir desde la raíz del repositorio:
#   docker build -f services/gateway/Dockerfile -t unillanos/gateway .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 1) Solo los proyectos, para cachear el restore.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY services/gateway/src/Gateway.Api/Gateway.Api.csproj services/gateway/src/Gateway.Api/
RUN dotnet restore services/gateway/src/Gateway.Api/Gateway.Api.csproj

# 2) El código y la publicación.
COPY services/gateway/src/ services/gateway/src/
RUN dotnet publish services/gateway/src/Gateway.Api/Gateway.Api.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
# Usuario sin privilegios que trae la imagen oficial.
USER $APP_UID
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Gateway.Api.dll"]
