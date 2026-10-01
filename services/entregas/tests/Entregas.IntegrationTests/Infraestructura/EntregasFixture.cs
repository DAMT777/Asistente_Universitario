using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;
using Unillanos.Pruebas.Compartidas;
using WireMock.Server;

namespace Entregas.IntegrationTests.Infraestructura;

[CollectionDefinition(Nombre)]
public sealed class ColeccionEntregas : ICollectionFixture<EntregasFixture>
{
    public const string Nombre = "entregas";
}

/// <summary>
/// Un SQL Server real (Testcontainers) para toda la colección y dos instancias del servicio:
/// <see cref="Api"/> con un doble de Evaluaciones y <see cref="ApiConWireMock"/> con el cliente HTTP real contra WireMock.
/// </summary>
public sealed class EntregasFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _sql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public LlavesPrueba Llaves { get; } = new();
    public WireMockServer WireMock { get; private set; } = null!;
    public EntregasApiFactory Api { get; private set; } = null!;
    public EntregasApiFactory ApiConWireMock { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _sql.StartAsync();
        WireMock = WireMockServer.Start();
        Api = new EntregasApiFactory(CadenaPara("entregas_db_pruebas"), Llaves, urlEvaluaciones: null);
        ApiConWireMock = new EntregasApiFactory(CadenaPara("entregas_db_wiremock"), Llaves, WireMock.Url);
    }

    public async ValueTask DisposeAsync()
    {
        await Api.DisposeAsync();
        await ApiConWireMock.DisposeAsync();
        WireMock.Stop();
        Llaves.Dispose();
        await _sql.DisposeAsync();
    }

    private string CadenaPara(string baseDatos)
        => new SqlConnectionStringBuilder(_sql.GetConnectionString()) { InitialCatalog = baseDatos }.ConnectionString;
}
