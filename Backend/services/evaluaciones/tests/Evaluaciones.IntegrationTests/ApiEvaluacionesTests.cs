using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;

namespace Evaluaciones.IntegrationTests;

/// <summary>
/// CU-09 a CU-12 de punta a punta: HTTP real, autenticación JWT, políticas por rol, EF Core y casos de uso.
/// Usan los datos semilla de la sección 9.8 de la guía técnica.
/// </summary>
public class ApiEvaluacionesTests
{
    private static async Task<JsonElement> Json(HttpResponseMessage r) =>
        (await r.Content.ReadFromJsonAsync<JsonElement>());

    private static string Codigo(JsonElement error) => error.GetProperty("codigo").GetString()!;

    // ───────── Autenticación y autorización ─────────

    [Fact]
    public async Task Sin_token_responde_401_con_el_formato_de_error_comun()
    {
        await using var f = new FabricaApi();

        var r = await f.CreateClient().GetAsync("/cursos");

        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
        var error = await Json(r);
        Assert.Equal("NO_AUTENTICADO", Codigo(error));
        Assert.Equal(401, error.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrEmpty(error.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task Token_expirado_responde_401_TOKEN_EXPIRADO()
    {
        await using var f = new FabricaApi();
        var cliente = f.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", f.Token(FabricaApi.Profesor, "PROFESOR", vigencia: TimeSpan.FromMinutes(-5)));

        var r = await cliente.GetAsync("/cursos");

        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
        Assert.Equal("TOKEN_EXPIRADO", Codigo(await Json(r)));
    }

    [Fact]
    public async Task Token_firmado_con_otra_llave_responde_401()
    {
        await using var f = new FabricaApi();
        using var impostora = RSA.Create(2048);
        var cliente = f.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", f.Token(FabricaApi.Profesor, "PROFESOR", firmadoCon: impostora));

        var r = await cliente.GetAsync("/cursos");

        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task Un_estudiante_no_puede_ver_el_ponderado_ni_publicar_cortes()
    {
        await using var f = new FabricaApi();
        var ana = f.ClienteEstudiante(FabricaApi.Ana);

        var ponderado = await ana.GetAsync($"/cursos/{FabricaApi.Curso}/ponderado");
        var publicar = await ana.PostAsJsonAsync($"/cursos/{FabricaApi.Curso}/cortes/1/publicar", new { });
        var corregir = await ana.PutAsJsonAsync($"/cursos/{FabricaApi.Curso}/cortes/1/estudiantes/{FabricaApi.Ana}", new { });

        Assert.Equal(HttpStatusCode.Forbidden, ponderado.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, publicar.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, corregir.StatusCode);
        Assert.Equal("SIN_PERMISO", Codigo(await Json(ponderado)));
    }

    [Fact]
    public async Task Otro_profesor_recibe_403_SIN_PERMISO_sobre_un_curso_ajeno()
    {
        // E-18
        await using var f = new FabricaApi();
        var otro = f.ClienteDe(FabricaApi.OtroProfesor, "PROFESOR");

        var r = await otro.PostAsJsonAsync($"/cursos/{FabricaApi.Curso}/cortes/1/publicar", new { });

        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
        Assert.Equal("SIN_PERMISO", Codigo(await Json(r)));
    }

    [Fact]
    public async Task Health_live_responde_sin_token()
    {
        await using var f = new FabricaApi();

        var r = await f.CreateClient().GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
    }

    // ───────── CU-12: cursos y actividades pendientes ─────────

    [Fact]
    public async Task CU12_el_profesor_ve_su_curso_con_id_y_pesos_de_cortes()
    {
        await using var f = new FabricaApi();

        var cursos = await Json(await f.ClienteProfesor().GetAsync("/cursos"));

        var curso = Assert.Single(cursos.EnumerateArray().ToList());
        Assert.Equal(FabricaApi.Curso.ToString(), curso.GetProperty("id").GetString());
        Assert.Equal("603803", curso.GetProperty("codigo").GetString());
        Assert.Equal("Profesor Demo", curso.GetProperty("profesor").GetString());
        Assert.Equal(30m, curso.GetProperty("pesoCorte1").GetDecimal());
        Assert.Equal(30m, curso.GetProperty("pesoCorte2").GetDecimal());
        Assert.Equal(40m, curso.GetProperty("pesoCorte3").GetDecimal());
        Assert.Equal(JsonValueKind.Null, curso.GetProperty("actividadesPendientes").ValueKind);
    }

    [Theory]
    [InlineData("b0000000-0000-0000-0000-000000000001", 1)] // Ana: solo falta el Proyecto
    [InlineData("b0000000-0000-0000-0000-000000000002", 3)] // Luis: su borrador no cuenta
    [InlineData("b0000000-0000-0000-0000-000000000003", 3)] // Marta: sin calificaciones
    public async Task CU12_el_estudiante_ve_su_curso_con_el_conteo_de_pendientes(string estudiante, int pendientes)
    {
        await using var f = new FabricaApi();

        var cursos = await Json(await f.ClienteEstudiante(Guid.Parse(estudiante)).GetAsync("/cursos"));

        var curso = Assert.Single(cursos.EnumerateArray().ToList());
        Assert.Equal(pendientes, curso.GetProperty("actividadesPendientes").GetInt32());
    }

    [Fact]
    public async Task CU12_actividades_del_estudiante_con_estado_vencida_y_filtro_de_pendientes()
    {
        await using var f = new FabricaApi();
        var ana = f.ClienteEstudiante(FabricaApi.Ana);

        var todas = (await Json(await ana.GetAsync($"/cursos/{FabricaApi.Curso}/actividades"))).EnumerateArray().ToList();
        var pendientes = (await Json(await ana.GetAsync($"/cursos/{FabricaApi.Curso}/actividades?soloPendientes=true"))).EnumerateArray().ToList();

        Assert.Equal(new[] { "Taller 1", "Parcial 1", "Proyecto 1" }, todas.Select(a => a.GetProperty("titulo").GetString()));
        Assert.All(todas, a => Assert.Equal(FabricaApi.Curso.ToString(), a.GetProperty("cursoId").GetString()));
        Assert.Equal("CALIFICADA", todas[0].GetProperty("estado").GetString());
        var proyecto = todas[2];
        Assert.Equal("PENDIENTE", proyecto.GetProperty("estado").GetString());
        Assert.True(proyecto.GetProperty("vencida").GetBoolean());
        Assert.EndsWith("Z", proyecto.GetProperty("fechaLimite").GetString()); // fechas en UTC (DA-10)
        Assert.Equal(JsonValueKind.Null, todas[1].GetProperty("fechaLimite").ValueKind);

        Assert.Equal("Proyecto 1", Assert.Single(pendientes).GetProperty("titulo").GetString());
    }

    [Fact]
    public async Task CU12_un_estudiante_no_inscrito_no_ve_el_curso()
    {
        // RN-16: el curso no existe para quien no está inscrito.
        await using var f = new FabricaApi();
        var extrano = f.ClienteEstudiante(Guid.NewGuid());

        var cursos = await Json(await extrano.GetAsync("/cursos"));
        var actividades = await extrano.GetAsync($"/cursos/{FabricaApi.Curso}/actividades");

        Assert.Empty(cursos.EnumerateArray());
        Assert.Equal(HttpStatusCode.NotFound, actividades.StatusCode);
        Assert.Equal("NO_ENCONTRADO", Codigo(await Json(actividades)));
    }

    // ───────── CU-09: ponderado ─────────

    [Fact]
    public async Task CU09_ponderado_de_Ana_tiene_corte_1_en_3_2_y_definitiva_parcial_1_0()
    {
        // E-13
        await using var f = new FabricaApi();

        var ana = await Json(await f.ClienteProfesor().GetAsync($"/cursos/{FabricaApi.Curso}/ponderado/{FabricaApi.Ana}"));

        var corte1 = ana.GetProperty("cortes").EnumerateArray().First();
        Assert.Equal(3.2m, corte1.GetProperty("notaCorte").GetDecimal());
        Assert.True(corte1.GetProperty("publicado").GetBoolean());
        Assert.Equal(1.0m, ana.GetProperty("definitivaParcial").GetDecimal());
        Assert.True(ana.GetProperty("esParcial").GetBoolean());
        Assert.Equal(2, corte1.GetProperty("actividades").GetArrayLength());

        var corte2 = ana.GetProperty("cortes").EnumerateArray().ElementAt(1);
        Assert.Equal(JsonValueKind.Null, corte2.GetProperty("notaCorte").ValueKind); // sin calificar no es 0
    }

    [Fact]
    public async Task CU09_ponderado_del_curso_trae_a_todos_y_las_publicaciones()
    {
        await using var f = new FabricaApi();

        var curso = await Json(await f.ClienteProfesor().GetAsync($"/cursos/{FabricaApi.Curso}/ponderado"));

        Assert.Equal(3, curso.GetProperty("estudiantes").GetArrayLength());
        var publicacion = Assert.Single(curso.GetProperty("publicaciones").EnumerateArray().ToList());
        Assert.Equal(FabricaApi.Ana.ToString(), publicacion.GetProperty("estudianteId").GetString());
        Assert.Equal(1, publicacion.GetProperty("corte").GetInt32());
        Assert.Equal(3.2m, publicacion.GetProperty("nota").GetDecimal());
        Assert.EndsWith("Z", publicacion.GetProperty("fechaPublicacion").GetString());
    }

    [Fact]
    public async Task Apoyo_el_profesor_lista_los_estudiantes_y_las_calificaciones_de_la_actividad()
    {
        await using var f = new FabricaApi();
        var profesor = f.ClienteProfesor();

        var estudiantes = (await Json(await profesor.GetAsync($"/cursos/{FabricaApi.Curso}/estudiantes"))).EnumerateArray().ToList();
        var calificaciones = (await Json(await profesor.GetAsync($"/actividades/{FabricaApi.Taller1}/calificaciones"))).EnumerateArray().ToList();

        Assert.Equal(new[] { "Ana Demo", "Luis Demo", "Marta Demo" }, estudiantes.Select(e => e.GetProperty("nombre").GetString()));
        Assert.All(estudiantes, e => Assert.Equal("ESTUDIANTE", e.GetProperty("rol").GetString()));
        Assert.Equal(2, calificaciones.Count);
        Assert.Equal("BORRADOR", calificaciones.Single(c => c.GetProperty("estudianteId").GetString() == FabricaApi.Luis.ToString()).GetProperty("estado").GetString());
    }

    // ───────── CU-10: publicar corte ─────────

    [Fact]
    public async Task CU10_publicar_el_corte_1_rechaza_a_Luis_por_borrador_y_a_Marta_por_no_tener_notas()
    {
        // E-12
        await using var f = new FabricaApi();
        var profesor = f.ClienteProfesor();

        var r = await profesor.PostAsJsonAsync($"/cursos/{FabricaApi.Curso}/cortes/1/publicar", new { estudiantes = (Guid[]?)null, omitirBorradores = false });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var resultado = await Json(r);
        Assert.Equal(1, resultado.GetProperty("corte").GetInt32());
        var publicado = Assert.Single(resultado.GetProperty("publicados").EnumerateArray().ToList());
        Assert.Equal(FabricaApi.Ana.ToString(), publicado.GetProperty("estudianteId").GetString());
        Assert.Equal(3.2m, publicado.GetProperty("nota").GetDecimal());

        var rechazados = resultado.GetProperty("rechazados").EnumerateArray().ToDictionary(
            x => x.GetProperty("estudianteId").GetString()!, x => x.GetProperty("codigo").GetString());
        Assert.Equal("CALIFICACIONES_EN_BORRADOR", rechazados[FabricaApi.Luis.ToString()]);
        Assert.Equal("SIN_CALIFICACIONES", rechazados[FabricaApi.Marta.ToString()]);
    }

    [Fact]
    public async Task CU10_publicar_dos_veces_no_duplica_la_publicacion()
    {
        await using var f = new FabricaApi();
        var profesor = f.ClienteProfesor();
        var ruta = $"/cursos/{FabricaApi.Curso}/cortes/1/publicar";

        await profesor.PostAsJsonAsync(ruta, new { });
        await profesor.PostAsJsonAsync(ruta, new { });

        var curso = await Json(await profesor.GetAsync($"/cursos/{FabricaApi.Curso}/ponderado"));
        Assert.Single(curso.GetProperty("publicaciones").EnumerateArray().ToList());
    }

    [Fact]
    public async Task CU10_un_cuerpo_vacio_publica_para_todos_los_inscritos()
    {
        await using var f = new FabricaApi();

        var r = await f.ClienteProfesor().PostAsync($"/cursos/{FabricaApi.Curso}/cortes/1/publicar", content: null);

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal(1, (await Json(r)).GetProperty("publicados").GetArrayLength());
    }

    [Fact]
    public async Task CU10_un_corte_fuera_de_1_a_3_responde_400()
    {
        await using var f = new FabricaApi();

        var r = await f.ClienteProfesor().PostAsJsonAsync($"/cursos/{FabricaApi.Curso}/cortes/4/publicar", new { });

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal("VALIDACION_FALLIDA", Codigo(await Json(r)));
    }

    [Fact]
    public async Task CU10_un_curso_inexistente_responde_404()
    {
        await using var f = new FabricaApi();

        var r = await f.ClienteProfesor().PostAsJsonAsync($"/cursos/{Guid.NewGuid()}/cortes/1/publicar", new { });

        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    // ───────── CU-11: corregir corte ─────────

    [Fact]
    public async Task CU11_corregir_a_Ana_recalcula_solo_su_publicacion()
    {
        // E-15
        await using var f = new FabricaApi();
        var profesor = f.ClienteProfesor();

        var r = await profesor.PutAsJsonAsync($"/cursos/{FabricaApi.Curso}/cortes/1/estudiantes/{FabricaApi.Ana}", new { omitirBorradores = false });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var publicacion = await Json(r);
        Assert.Equal(FabricaApi.Ana.ToString(), publicacion.GetProperty("estudianteId").GetString());
        Assert.Equal(3.2m, publicacion.GetProperty("nota").GetDecimal());

        var curso = await Json(await profesor.GetAsync($"/cursos/{FabricaApi.Curso}/ponderado"));
        Assert.Single(curso.GetProperty("publicaciones").EnumerateArray().ToList()); // las demás siguen sin publicar
    }

    [Fact]
    public async Task CU11_corregir_un_corte_que_no_esta_publicado_responde_404()
    {
        await using var f = new FabricaApi();

        var r = await f.ClienteProfesor().PutAsJsonAsync($"/cursos/{FabricaApi.Curso}/cortes/1/estudiantes/{FabricaApi.Marta}", new { });

        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
        Assert.Equal("NO_ENCONTRADO", Codigo(await Json(r)));
    }

    [Fact]
    public async Task CU11_un_encabezado_if_match_mal_formado_responde_400()
    {
        await using var f = new FabricaApi();
        var profesor = f.ClienteProfesor();
        var solicitud = new HttpRequestMessage(HttpMethod.Put, $"/cursos/{FabricaApi.Curso}/cortes/1/estudiantes/{FabricaApi.Ana}")
        {
            Content = JsonContent.Create(new { })
        };
        solicitud.Headers.TryAddWithoutValidation("If-Match", "\"esto no es base64!\"");

        var r = await profesor.SendAsync(solicitud);

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal("VALIDACION_FALLIDA", Codigo(await Json(r)));
    }
}
