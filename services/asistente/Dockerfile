# Imagen del servicio de Asistente. Construir desde la raíz del repositorio:
#   docker build -f services/asistente/Dockerfile -t unillanos/asistente .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 1) Solo los proyectos, para cachear el restore.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY services/compartido/Unillanos.ServiceDefaults/Unillanos.ServiceDefaults.csproj services/compartido/Unillanos.ServiceDefaults/
COPY services/asistente/src/Asistente.Domain/Asistente.Domain.csproj services/asistente/src/Asistente.Domain/
COPY services/asistente/src/Asistente.Application/Asistente.Application.csproj services/asistente/src/Asistente.Application/
COPY services/asistente/src/Asistente.Infrastructure/Asistente.Infrastructure.csproj services/asistente/src/Asistente.Infrastructure/
COPY services/asistente/src/Asistente.Api/Asistente.Api.csproj services/asistente/src/Asistente.Api/
RUN dotnet restore services/asistente/src/Asistente.Api/Asistente.Api.csproj

# 2) El código y la publicación.
COPY services/compartido/Unillanos.ServiceDefaults/ services/compartido/Unillanos.ServiceDefaults/
COPY services/asistente/src/ services/asistente/src/
RUN dotnet publish services/asistente/src/Asistente.Api/Asistente.Api.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
# Usuario sin privilegios que trae la imagen oficial.
USER $APP_UID
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Asistente.Api.dll"]
