using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Unillanos.Pruebas.Compartidas.Contratos;

namespace Evaluaciones.IntegrationTests;

/// <summary>
/// CU-15, CU-16 y la consulta interna de punta a punta (HTTP, JWT, roles, EF Core), con cada respuesta
/// validada contra contracts/evaluaciones.yaml.
/// </summary>
public class MisNotasApiTests
{
    private static readonly ContratoOpenApi Contrato = ContratoOpenApi.Cargar("evaluaciones.yaml");
    private const string RutaActividades = "/mis-notas/cursos/{cursoId}/actividades";
    private const string RutaInterna = "/internal/actividades/{actividadId}";

    private static async Task<JsonElement> Json(HttpResponseMessage r) => await r.Content.ReadFromJsonAsync<JsonElement>();

    [Fact]
    public async Task E13_Ana_corte_1_3_2_y_definitiva_1_0()
    {
        await using var f = new FabricaApi();

        var r = await f.ClienteEstudiante(FabricaApi.Ana).GetAsync("/mis-notas");

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        await r.CumpleContratoAsync(Contrato, "get", "/mis-notas");
        var curso = (await Json(r)).GetProperty("cursos")[0];
        var cortes = curso.GetProperty("cortes");
        Assert.Equal(3.2m, cortes[0].GetProperty("nota").GetDecimal());
        Assert.True(cortes[0].GetProperty("publicado").GetBoolean());
        Assert.Equal(JsonValueKind.Null, cortes[1].GetProperty("nota").ValueKind);
        Assert.Equal(1.0m, curso.GetProperty("definitivaParcial").GetDecimal());
        Assert.True(curso.GetProperty("esParcial").GetBoolean());
    }

    [Fact]
    public async Task E14_Marta_cortes_null_y_definitiva_0()
    {
        await using var f = new FabricaApi();

        var r = await f.ClienteEstudiante(FabricaApi.Marta).GetAsync("/mis-notas");

        await r.CumpleContratoAsync(Contrato, "get", "/mis-notas");
        var curso = (await Json(r)).GetProperty("cursos")[0];
        Assert.All(curso.GetProperty("cortes").EnumerateArray(), c => Assert.False(c.GetProperty("publicado").GetBoolean()));
        Assert.Equal(0m, curso.GetProperty("definitivaParcial").GetDecimal());
    }

    [Fact]
    public async Task E07_Luis_ve_su_borrador_como_SIN_CALIFICAR_sin_el_valor()
    {
        await using var f = new FabricaApi();

        var r = await f.ClienteEstudiante(FabricaApi.Luis).GetAsync($"/mis-notas/cursos/{FabricaApi.Curso}/actividades");

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        await r.CumpleContratoAsync(Contrato, "get", RutaActividades);
        Assert.DoesNotContain("2.5", await r.Content.ReadAsStringAsync());
        var taller = (await Json(r)).GetProperty("actividades").EnumerateArray().Single(a => a.GetProperty("titulo").GetString() == "Taller 1");
        Assert.Equal("SIN_CALIFICAR", taller.GetProperty("estado").GetString());
        Assert.Equal(JsonValueKind.Null, taller.GetProperty("nota").ValueKind);
    }

    [Fact]
    public async Task Ana_ve_notas_publicadas_con_retroalimentacion_y_actividad_sin_fecha()
    {
        await using var f = new FabricaApi();

        var r = await f.ClienteEstudiante(FabricaApi.Ana).GetAsync($"/mis-notas/cursos/{FabricaApi.Curso}/actividades");

        await r.CumpleContratoAsync(Contrato, "get", RutaActividades);
        var act = (await Json(r)).GetProperty("actividades").EnumerateArray().ToDictionary(a => a.GetProperty("titulo").GetString()!);
        Assert.Equal(4.0m, act["Taller 1"].GetProperty("nota").GetDecimal());
        Assert.Equal("Buen trabajo.", act["Taller 1"].GetProperty("retroalimentacion").GetString());
        Assert.Equal(JsonValueKind.Null, act["Parcial 1"].GetProperty("fechaLimite").ValueKind);
        Assert.Equal("SIN_CALIFICAR", act["Proyecto 1"].GetProperty("estado").GetString());
    }

    [Fact]
    public async Task No_inscrito_403_y_curso_inexistente_404()
    {
        await using var f = new FabricaApi();

        var noInscrito = await f.ClienteEstudiante(Guid.NewGuid()).GetAsync($"/mis-notas/cursos/{FabricaApi.Curso}/actividades");
        var inexistente = await f.ClienteEstudiante(FabricaApi.Ana).GetAsync($"/mis-notas/cursos/{Guid.NewGuid()}/actividades");

        Assert.Equal(HttpStatusCode.Forbidden, noInscrito.StatusCode);
        Assert.Equal("SIN_PERMISO", (await Json(noInscrito)).GetProperty("codigo").GetString());
        await noInscrito.CumpleContratoAsync(Contrato, "get", RutaActividades);
        Assert.Equal(HttpStatusCode.NotFound, inexistente.StatusCode);
        await inexistente.CumpleContratoAsync(Contrato, "get", RutaActividades);
    }

    [Fact]
    public async Task Un_profesor_no_usa_endpoints_de_estudiante_y_sin_token_es_401()
    {
        await using var f = new FabricaApi();

        var profesor = await f.ClienteProfesor().GetAsync("/mis-notas");
        var sinToken = await f.CreateClient().GetAsync("/mis-notas");

        Assert.Equal(HttpStatusCode.Forbidden, profesor.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, sinToken.StatusCode);
        await sinToken.CumpleContratoAsync(Contrato, "get", "/mis-notas");
    }

    [Fact]
    public async Task Interno_con_llave_devuelve_la_actividad_y_sin_llave_401()
    {
        await using var f = new FabricaApi();
        var conLlave = f.CreateClient();
        conLlave.DefaultRequestHeaders.Add("X-Service-Key", FabricaApi.ClaveServicio);
        var url = $"/internal/actividades/{FabricaApi.Taller1}?estudianteId={FabricaApi.Ana}";

        var ok = await conLlave.GetAsync(url);
        var sinEstudiante = await conLlave.GetAsync($"/internal/actividades/{FabricaApi.Taller1}");
        var sinLlave = await f.ClienteEstudiante(FabricaApi.Ana).GetAsync(url);
        var noExiste = await conLlave.GetAsync($"/internal/actividades/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        await ok.CumpleContratoAsync(Contrato, "get", RutaInterna);
        var a = await Json(ok);
        Assert.Equal(FabricaApi.Profesor, a.GetProperty("profesorId").GetGuid());
        Assert.True(a.GetProperty("estudianteInscrito").GetBoolean());
        Assert.False((await Json(sinEstudiante)).GetProperty("estudianteInscrito").GetBoolean());
        Assert.Equal(HttpStatusCode.Unauthorized, sinLlave.StatusCode);
        await sinLlave.CumpleContratoAsync(Contrato, "get", RutaInterna);
        Assert.Equal(HttpStatusCode.NotFound, noExiste.StatusCode);
    }

    [Fact]
    public async Task Las_operaciones_del_contrato_estan_implementadas()
    {
        await using var f = new FabricaApi();
        var generado = JsonNode.Parse(await f.CreateClient().GetStringAsync("/openapi/v1.json"))!;

        // El servicio expone además los endpoints del profesor (CU-09 a CU-12), que no están en este contrato.
        var faltantes = Contrato.Operaciones()
            .Where(o => !o.Ruta.StartsWith("/health/", StringComparison.Ordinal))
            .Where(o => generado["paths"]?[o.Ruta]?[o.Metodo] is null)
            .ToList();
        Assert.Empty(faltantes);
    }
}
