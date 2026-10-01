using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Evaluaciones.Infrastructure.Persistencia;
using Evaluaciones.IntegrationTests.Infraestructura;
using Unillanos.Pruebas.Compartidas;
using Unillanos.Pruebas.Compartidas.Contratos;

namespace Evaluaciones.IntegrationTests;

[Collection(ColeccionEvaluaciones.Nombre)]
public sealed class SeguridadYContratoTests(EvaluacionesFixture fixture)
{
    private static readonly ContratoOpenApi Contrato = ContratoOpenApi.Cargar("evaluaciones.yaml");

    [Fact]
    public async Task Sin_token_responde_401_NO_AUTENTICADO()
    {
        var respuesta = await fixture.Api.CreateClient().GetAsync("/mis-notas");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Equal("NO_AUTENTICADO", (await respuesta.Content.ReadFromJsonAsync<ErrorDto>())!.Codigo);
        await respuesta.CumpleContratoAsync(Contrato, "get", "/mis-notas");
    }

    [Fact]
    public async Task Token_expirado_responde_401_TOKEN_EXPIRADO()
    {
        var respuesta = await fixture.ClienteDe(UsuariosSemilla.Ana, expirado: true).GetAsync("/mis-notas");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Equal("TOKEN_EXPIRADO", (await respuesta.Content.ReadFromJsonAsync<ErrorDto>())!.Codigo);
    }

    [Theory]
    [InlineData("/mis-notas")]
    [InlineData("/mis-notas/cursos/c0000000-0000-0000-0000-000000000001/actividades")]
    public async Task Rol_PROFESOR_en_endpoints_de_estudiante_responde_403(string url)
    {
        var respuesta = await fixture.ClienteDe(UsuariosSemilla.Profesor, rol: "PROFESOR").GetAsync(url);

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Equal("SIN_PERMISO", (await respuesta.Content.ReadFromJsonAsync<ErrorDto>())!.Codigo);
    }

    [Fact]
    public async Task Health_live_y_ready_responden_200_sin_token()
    {
        var cliente = fixture.Api.CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/health/live")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/health/ready")).StatusCode);
    }

    [Fact]
    public async Task La_semilla_es_idempotente()
    {
        await using var scope = fixture.Api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<EvaluacionesDbContext>();
        var antes = (await db.Actividades.CountAsync(), await db.Calificaciones.CountAsync(), await db.PublicacionesCorte.CountAsync(), await db.CursoEstudiantes.CountAsync());

        await scope.ServiceProvider.GetRequiredService<SemillaDesarrollo>().CargarAsync(CancellationToken.None);

        var despues = (await db.Actividades.CountAsync(), await db.Calificaciones.CountAsync(), await db.PublicacionesCorte.CountAsync(), await db.CursoEstudiantes.CountAsync());
        Assert.Equal((3, 3, 1, 3), antes);
        Assert.Equal(antes, despues);
    }

    [Fact]
    public async Task Todas_las_operaciones_del_contrato_estan_implementadas_y_no_hay_extras()
    {
        var generado = JsonNode.Parse(await fixture.Api.CreateClient().GetStringAsync("/openapi/v1.json"))!;
        var delContrato = Contrato.Operaciones().Where(o => !o.Ruta.StartsWith("/health/", StringComparison.Ordinal)).ToHashSet();
        var implementadas = generado["paths"]!.AsObject()
            .SelectMany(p => p.Value!.AsObject().Select(m => (Ruta: p.Key, Metodo: m.Key)))
            .ToHashSet();

        Assert.Empty(delContrato.Except(implementadas));
        Assert.Empty(implementadas.Except(delContrato));
    }
}
