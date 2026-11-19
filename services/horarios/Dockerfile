FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY services/compartido/Unillanos.ServiceDefaults/Unillanos.ServiceDefaults.csproj services/compartido/Unillanos.ServiceDefaults/
COPY services/horarios/src/Horarios.Api/Horarios.Api.csproj services/horarios/src/Horarios.Api/
RUN dotnet restore services/horarios/src/Horarios.Api/Horarios.Api.csproj
COPY services/compartido/Unillanos.ServiceDefaults/ services/compartido/Unillanos.ServiceDefaults/
COPY services/horarios/src/ services/horarios/src/
RUN dotnet publish services/horarios/src/Horarios.Api/Horarios.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Horarios.Api.dll"]
