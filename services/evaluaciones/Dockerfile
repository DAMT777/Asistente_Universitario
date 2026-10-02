# Imagen del servicio de Evaluaciones. Construir desde la raíz del repositorio:
#   docker build -f services/evaluaciones/Dockerfile -t unillanos/evaluaciones .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 1) Solo los proyectos, para cachear el restore.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY services/evaluaciones/src/Evaluaciones.Domain/Evaluaciones.Domain.csproj services/evaluaciones/src/Evaluaciones.Domain/
COPY services/evaluaciones/src/Evaluaciones.Application/Evaluaciones.Application.csproj services/evaluaciones/src/Evaluaciones.Application/
COPY services/evaluaciones/src/Evaluaciones.Infrastructure/Evaluaciones.Infrastructure.csproj services/evaluaciones/src/Evaluaciones.Infrastructure/
COPY services/evaluaciones/src/Evaluaciones.Api/Evaluaciones.Api.csproj services/evaluaciones/src/Evaluaciones.Api/
RUN dotnet restore services/evaluaciones/src/Evaluaciones.Api/Evaluaciones.Api.csproj

# 2) El código y la publicación.
COPY services/evaluaciones/src/ services/evaluaciones/src/
RUN dotnet publish services/evaluaciones/src/Evaluaciones.Api/Evaluaciones.Api.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
# Usuario sin privilegios que trae la imagen oficial.
USER $APP_UID
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Evaluaciones.Api.dll"]
