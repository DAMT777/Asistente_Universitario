# Imagen del servicio de Entregas. Construir desde la raíz del repositorio:
#   docker build -f services/entregas/Dockerfile -t unillanos/entregas .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 1) Solo los proyectos, para cachear el restore.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY services/compartido/Unillanos.ServiceDefaults/Unillanos.ServiceDefaults.csproj services/compartido/Unillanos.ServiceDefaults/
COPY services/entregas/src/Unillanos.Entregas.Domain/Unillanos.Entregas.Domain.csproj services/entregas/src/Unillanos.Entregas.Domain/
COPY services/entregas/src/Unillanos.Entregas.Application/Unillanos.Entregas.Application.csproj services/entregas/src/Unillanos.Entregas.Application/
COPY services/entregas/src/Unillanos.Entregas.Infrastructure/Unillanos.Entregas.Infrastructure.csproj services/entregas/src/Unillanos.Entregas.Infrastructure/
COPY services/entregas/src/Unillanos.Entregas.Api/Unillanos.Entregas.Api.csproj services/entregas/src/Unillanos.Entregas.Api/
RUN dotnet restore services/entregas/src/Unillanos.Entregas.Api/Unillanos.Entregas.Api.csproj

# 2) El código y la publicación.
COPY services/compartido/Unillanos.ServiceDefaults/ services/compartido/Unillanos.ServiceDefaults/
COPY services/entregas/src/ services/entregas/src/
RUN dotnet publish services/entregas/src/Unillanos.Entregas.Api/Unillanos.Entregas.Api.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
# Usuario sin privilegios que trae la imagen oficial.
USER $APP_UID
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Unillanos.Entregas.Api.dll"]
