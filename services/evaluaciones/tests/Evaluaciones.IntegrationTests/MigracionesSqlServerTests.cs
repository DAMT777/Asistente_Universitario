using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace Evaluaciones.IntegrationTests;

/// <summary>
/// El servicio contra SQL Server real (Testcontainers): las migraciones crean el esquema, la semilla de
/// desarrollo carga los datos de la guía y los endpoints del estudiante y del profesor responden.
/// Requiere Docker.
/// </summary>
public sealed class MigracionesSqlServerTests : IAsyncLifetime
{
    private readonly MsSqlContainer _sql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public async ValueTask InitializeAsync() => await _sql.StartAsync();

    public async ValueTask DisposeAsync() => await _sql.DisposeAsync();

    [Fact]
    public async Task Migra_siembra_y_responde_con_SQL_Server()
    {
        var cadena = new SqlConnectionStringBuilder(_sql.GetConnectionString()) { InitialCatalog = "evaluaciones_db" }.ConnectionString;
        await using var f = new FabricaApi(cadena);

        var matriz = await f.ClienteEstudiante(FabricaApi.Ana).GetAsync("/mis-notas");
        var cursosProfesor = await f.ClienteProfesor().GetAsync("/cursos");
        var ponderado = await f.ClienteProfesor().GetAsync($"/cursos/{FabricaApi.Curso}/ponderado");

        Assert.Equal(HttpStatusCode.OK, matriz.StatusCode);
        var curso = (await matriz.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("cursos")[0];
        Assert.Equal(1.0m, curso.GetProperty("definitivaParcial").GetDecimal());
        Assert.Equal(HttpStatusCode.OK, cursosProfesor.StatusCode);
        Assert.Equal(HttpStatusCode.OK, ponderado.StatusCode);
    }
}
