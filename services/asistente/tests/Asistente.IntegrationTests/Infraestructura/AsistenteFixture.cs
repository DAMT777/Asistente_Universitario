using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;
using Unillanos.Pruebas.Compartidas;
using WireMock.Server;

namespace Asistente.IntegrationTests.Infraestructura;

[CollectionDefinition(Nombre)]
public sealed class ColeccionAsistente : ICollectionFixture<AsistenteFixture>
{
    public const string Nombre = "asistente";
}

/// <summary>Un SQL Server (Testcontainers), un WireMock como gateway, otro como Groq y dos instancias del servicio.</summary>
public sealed class AsistenteFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _sql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public LlavesPrueba Llaves { get; } = new();
    public WireMockServer Gateway { get; private set; } = null!;
    public WireMockServer Groq { get; private set; } = null!;
    public AsistenteApiFactory Api { get; private set; } = null!;
    public AsistenteApiFactory ApiConGroq { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _sql.StartAsync();
        Gateway = WireMockServer.Start();
        Groq = WireMockServer.Start();
        Api = new AsistenteApiFactory(CadenaPara("asistente_db_pruebas"), Llaves, Gateway.Url!, urlLlm: null);
        ApiConGroq = new AsistenteApiFactory(CadenaPara("asistente_db_groq"), Llaves, Gateway.Url!, Groq.Url + "/openai/v1");
    }

    public async ValueTask DisposeAsync()
    {
        await Api.DisposeAsync();
        await ApiConGroq.DisposeAsync();
        Gateway.Stop();
        Groq.Stop();
        Llaves.Dispose();
        await _sql.DisposeAsync();
    }

    private string CadenaPara(string baseDatos) =>
        new SqlConnectionStringBuilder(_sql.GetConnectionString()) { InitialCatalog = baseDatos }.ConnectionString;
}
