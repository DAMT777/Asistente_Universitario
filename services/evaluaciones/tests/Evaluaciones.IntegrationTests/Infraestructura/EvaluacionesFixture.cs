using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;
using Unillanos.Pruebas.Compartidas;

namespace Evaluaciones.IntegrationTests.Infraestructura;

[CollectionDefinition(Nombre)]
public sealed class ColeccionEvaluaciones : ICollectionFixture<EvaluacionesFixture>
{
    public const string Nombre = "evaluaciones";
}

/// <summary>SQL Server real (Testcontainers) con migraciones y semilla de Development aplicadas al arrancar.</summary>
public sealed class EvaluacionesFixture : IAsyncLifetime
{
    public const string ClaveServicio = "clave-de-servicio-solo-para-pruebas";
    private readonly MsSqlContainer _sql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public LlavesPrueba Llaves { get; } = new();
    public EvaluacionesApiFactory Api { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _sql.StartAsync();
        var cadena = new SqlConnectionStringBuilder(_sql.GetConnectionString()) { InitialCatalog = "evaluaciones_db_pruebas" }.ConnectionString;
        Api = new EvaluacionesApiFactory(cadena, Llaves);
    }

    public async ValueTask DisposeAsync()
    {
        await Api.DisposeAsync();
        Llaves.Dispose();
        await _sql.DisposeAsync();
    }

    public HttpClient ClienteDe(Guid usuarioId, string rol = "ESTUDIANTE", bool expirado = false)
    {
        var cliente = Api.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Llaves.CrearToken(usuarioId, rol, "Usuario", expirado));
        return cliente;
    }
}

public sealed class EvaluacionesApiFactory(string cadenaSql, LlavesPrueba llaves) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Default", cadenaSql);
        builder.UseSetting("Jwt:PublicKeyPath", llaves.RutaLlavePublica);
        builder.UseSetting("ServiceKey", EvaluacionesFixture.ClaveServicio);
        builder.UseSetting("Database:AplicarMigraciones", "true");
    }
}
