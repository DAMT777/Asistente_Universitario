# Imagen del servicio de Usuarios. Construir desde la raíz del repositorio:
#   docker build -f services/usuarios/Dockerfile -t unillanos/usuarios .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 1) Solo los proyectos, para cachear el restore.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY services/usuarios/src/Usuarios.Api/Usuarios.Api.csproj services/usuarios/src/Usuarios.Api/
RUN dotnet restore services/usuarios/src/Usuarios.Api/Usuarios.Api.csproj

# 2) El código y la publicación.
COPY services/usuarios/src/ services/usuarios/src/
RUN dotnet publish services/usuarios/src/Usuarios.Api/Usuarios.Api.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
# Usuario sin privilegios que trae la imagen oficial.
USER $APP_UID
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Usuarios.Api.dll"]
