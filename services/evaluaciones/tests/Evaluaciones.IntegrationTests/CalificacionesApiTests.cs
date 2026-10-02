using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Evaluaciones.IntegrationTests;

/// <summary>
/// CU-05 a CU-08 de punta a punta, y lo que el estudiante ve después (CU-15, CU-16).
/// Usan los datos semilla de la sección 9.8 de la guía técnica.
/// </summary>
public class CalificacionesApiTests
{
    private static readonly Guid EntregaProyectoAna = new("e0000000-0000-0000-0000-000000000003");

    private static async Task<JsonElement> Json(HttpResponseMessage r) =>
        await r.Content.ReadFromJsonAsync<JsonElement>();

    private static string Codigo(JsonElement error) => error.GetProperty("codigo").GetString()!;

    /// <summary>Filas PUBLICADAS de CU-15 (las demás salen como SIN_CALIFICAR con nota null).</summary>
    private static async Task<List<JsonElement>> PublicadasAsync(HttpClient estudiante) =>
        (await Json(await estudiante.GetAsync($"/mis-notas/cursos/{FabricaApi.Curso}/actividades")))
            .GetProperty("actividades").EnumerateArray()
            .Where(a => a.GetProperty("estado").GetString() == "PUBLICADA").ToList();

    private static Task<HttpResponseMessage> Calificar(HttpClient cliente, Guid actividad, Guid estudiante, object cuerpo) =>
        cliente.PutAsJsonAsync($"/actividades/{actividad}/calificaciones/{estudiante}", cuerpo);

    [Fact]
    public async Task CU05_califica_una_entrega_con_nota_y_retroalimentacion_y_queda_en_borrador()
    {
        await using var f = new FabricaApi();

        var r = await Calificar(f.ClienteProfesor(), FabricaApi.Proyecto1, FabricaApi.Ana,
            new { valor = 4.5, retroalimentacion = "Buen análisis, falta justificar el supuesto 2.", entregaId = EntregaProyectoAna });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var c = await Json(r);
        Assert.Equal(4.5m, c.GetProperty("valor").GetDecimal());
        Assert.Equal("BORRADOR", c.GetProperty("estado").GetString());
        Assert.Equal(EntregaProyectoAna.ToString(), c.GetProperty("entregaId").GetString());
        Assert.False(string.IsNullOrEmpty(c.GetProperty("id").GetString()));
    }

    [Fact]
    public async Task CU06_registra_la_nota_de_un_parcial_sin_entrega()
    {
        await using var f = new FabricaApi();
        var profesor = f.ClienteProfesor();

        var r = await Calificar(profesor, FabricaApi.Parcial1, FabricaApi.Marta, new { valor = 0.0, retroalimentacion = "No presentó." });
        var lista = (await Json(await profesor.GetAsync($"/actividades/{FabricaApi.Parcial1}/calificaciones"))).EnumerateArray().ToList();

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var marta = lista.Single(c => c.GetProperty("estudianteId").GetString() == FabricaApi.Marta.ToString());
        Assert.Equal(0.0m, marta.GetProperty("valor").GetDecimal()); // E-09: 0.0 es distinto de sin calificar
        Assert.Equal("BORRADOR", marta.GetProperty("estado").GetString());
    }

    [Fact]
    public async Task CU06_indicar_entrega_en_una_actividad_sin_entrega_responde_422()
    {
        await using var f = new FabricaApi();

        var r = await Calificar(f.ClienteProfesor(), FabricaApi.Parcial1, FabricaApi.Marta, new { valor = 3.0, entregaId = EntregaProyectoAna });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, r.StatusCode);
        Assert.Equal("ACTIVIDAD_SIN_ENTREGA", Codigo(await Json(r)));
    }

    [Fact]
    public async Task CU07_modificar_una_nota_publicada_la_deja_en_borrador_y_el_estudiante_deja_de_verla()
    {
        await using var f = new FabricaApi();
        var profesor = f.ClienteProfesor();
        var ana = f.ClienteEstudiante(FabricaApi.Ana);

        var r = await Calificar(profesor, FabricaApi.Taller1, FabricaApi.Ana, new { valor = 4.6, retroalimentacion = "Se revisó el reclamo." });
        var visibles = await PublicadasAsync(ana);

        Assert.Equal("BORRADOR", (await Json(r)).GetProperty("estado").GetString());
        Assert.DoesNotContain(visibles, n => n.GetProperty("actividadId").GetString() == FabricaApi.Taller1.ToString());
        var calificaciones = (await Json(await profesor.GetAsync($"/actividades/{FabricaApi.Taller1}/calificaciones"))).EnumerateArray().ToList();
        Assert.Single(calificaciones, c => c.GetProperty("estudianteId").GetString() == FabricaApi.Ana.ToString());
    }

    [Fact]
    public async Task Nota_fuera_de_rango_responde_422_NOTA_FUERA_DE_RANGO()
    {
        await using var f = new FabricaApi();

        var r = await Calificar(f.ClienteProfesor(), FabricaApi.Parcial1, FabricaApi.Marta, new { valor = 5.5 });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, r.StatusCode);
        Assert.Equal("NOTA_FUERA_DE_RANGO", Codigo(await Json(r)));
    }

    [Fact]
    public async Task Cuerpo_mal_formado_responde_400_con_el_formato_comun()
    {
        await using var f = new FabricaApi();
        var contenido = new StringContent("{\"valor\":\"abc\"}", Encoding.UTF8, "application/json");

        var r = await f.ClienteProfesor().PutAsync($"/actividades/{FabricaApi.Parcial1}/calificaciones/{FabricaApi.Marta}", contenido);

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal("VALIDACION_FALLIDA", Codigo(await Json(r)));
    }

    [Fact]
    public async Task Un_estudiante_no_puede_calificar_ni_publicar()
    {
        await using var f = new FabricaApi();
        var ana = f.ClienteEstudiante(FabricaApi.Ana);

        var calificar = await Calificar(ana, FabricaApi.Taller1, FabricaApi.Ana, new { valor = 5.0 });
        var publicar = await ana.PostAsync($"/actividades/{FabricaApi.Taller1}/calificaciones/publicar", content: null);

        Assert.Equal(HttpStatusCode.Forbidden, calificar.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, publicar.StatusCode);
    }

    [Fact]
    public async Task Otro_profesor_recibe_403_al_calificar_un_curso_ajeno()
    {
        // E-18
        await using var f = new FabricaApi();

        var r = await Calificar(f.ClienteDe(FabricaApi.OtroProfesor, "PROFESOR"), FabricaApi.Parcial1, FabricaApi.Marta, new { valor = 3.0 });

        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
        Assert.Equal("SIN_PERMISO", Codigo(await Json(r)));
    }

    [Fact]
    public async Task E08_el_estudiante_ve_la_nota_y_la_retroalimentacion_solo_despues_de_publicar()
    {
        await using var f = new FabricaApi();
        var profesor = f.ClienteProfesor();
        var marta = f.ClienteEstudiante(FabricaApi.Marta);
        await Calificar(profesor, FabricaApi.Parcial1, FabricaApi.Marta, new { valor = 4.5, retroalimentacion = "Muy bien." });
        var antes = await PublicadasAsync(marta);
        var publicacion = await Json(await profesor.PostAsync($"/actividades/{FabricaApi.Parcial1}/calificaciones/publicar", content: null));
        var despues = await PublicadasAsync(marta);

        Assert.Empty(antes);
        Assert.Equal(1, publicacion.GetProperty("publicadas").GetInt32());
        var nota = Assert.Single(despues);
        Assert.Equal(4.5m, nota.GetProperty("nota").GetDecimal());
        Assert.Equal("Muy bien.", nota.GetProperty("retroalimentacion").GetString());
        Assert.Equal("PUBLICADA", nota.GetProperty("estado").GetString());
    }

    [Fact]
    public async Task CU08_publica_el_borrador_de_Luis_en_el_taller()
    {
        await using var f = new FabricaApi();
        var profesor = f.ClienteProfesor();

        var r = await profesor.PostAsync($"/actividades/{FabricaApi.Taller1}/calificaciones/publicar", content: null);
        var lista = (await Json(await profesor.GetAsync($"/actividades/{FabricaApi.Taller1}/calificaciones"))).EnumerateArray().ToList();

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal(1, (await Json(r)).GetProperty("publicadas").GetInt32());
        Assert.All(lista, c => Assert.Equal("PUBLICADA", c.GetProperty("estado").GetString()));
    }

    [Fact]
    public async Task CU16_la_matriz_de_Ana_tiene_el_corte_1_y_los_demas_vacios()
    {
        // E-14 y RN-14
        await using var f = new FabricaApi();

        var matriz = await Json(await f.ClienteEstudiante(FabricaApi.Ana).GetAsync("/mis-notas"));

        var curso = Assert.Single(matriz.GetProperty("cursos").EnumerateArray().ToList());
        var cortes = curso.GetProperty("cortes").EnumerateArray().ToList();
        Assert.Equal(3.2m, cortes[0].GetProperty("nota").GetDecimal());
        Assert.Equal(JsonValueKind.Null, cortes[1].GetProperty("nota").ValueKind);
        Assert.Equal(1.0m, curso.GetProperty("definitivaParcial").GetDecimal());
        Assert.True(curso.GetProperty("esParcial").GetBoolean());
    }

    [Fact]
    public async Task CU16_el_profesor_no_usa_mis_notas()
    {
        await using var f = new FabricaApi();

        var r = await f.ClienteProfesor().GetAsync("/mis-notas");

        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
    }
}
