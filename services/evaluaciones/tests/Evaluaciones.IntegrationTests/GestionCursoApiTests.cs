using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Evaluaciones.IntegrationTests;

/// <summary>CU-02 y CU-03 de punta a punta, el detalle del curso y el endpoint interno de la sección 8.6.</summary>
public class GestionCursoApiTests
{
    private static async Task<JsonElement> Json(HttpResponseMessage r) => await r.Content.ReadFromJsonAsync<JsonElement>();
    private static string Codigo(JsonElement error) => error.GetProperty("codigo").GetString()!;

    [Fact]
    public async Task CU02_define_los_pesos_y_el_detalle_del_curso_los_refleja()
    {
        await using var f = new FabricaApi();
        var profesor = f.ClienteProfesor();

        var r = await profesor.PutAsJsonAsync($"/cursos/{FabricaApi.Curso}/pesos", new { pesoCorte1 = 25, pesoCorte2 = 35, pesoCorte3 = 40 });
        var curso = await Json(await profesor.GetAsync($"/cursos/{FabricaApi.Curso}"));

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal(25m, curso.GetProperty("pesoCorte1").GetDecimal());
        Assert.Equal(35m, curso.GetProperty("pesoCorte2").GetDecimal());
    }

    [Fact]
    public async Task CU02_E11_pesos_30_30_30_responden_422_PESOS_CORTE_INVALIDOS()
    {
        await using var f = new FabricaApi();

        var r = await f.ClienteProfesor().PutAsJsonAsync($"/cursos/{FabricaApi.Curso}/pesos", new { pesoCorte1 = 30, pesoCorte2 = 30, pesoCorte3 = 30 });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, r.StatusCode);
        Assert.Equal("PESOS_CORTE_INVALIDOS", Codigo(await Json(r)));
    }

    [Fact]
    public async Task CU02_un_estudiante_no_define_pesos_y_otro_profesor_tampoco()
    {
        await using var f = new FabricaApi();
        var cuerpo = new { pesoCorte1 = 25, pesoCorte2 = 35, pesoCorte3 = 40 };

        var estudiante = await f.ClienteEstudiante(FabricaApi.Ana).PutAsJsonAsync($"/cursos/{FabricaApi.Curso}/pesos", cuerpo);
        var otro = await f.ClienteDe(FabricaApi.OtroProfesor, "PROFESOR").PutAsJsonAsync($"/cursos/{FabricaApi.Curso}/pesos", cuerpo);

        Assert.Equal(HttpStatusCode.Forbidden, estudiante.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, otro.StatusCode);
    }

    [Fact]
    public async Task CU03_crea_una_actividad_con_201_y_la_lista_la_incluye()
    {
        await using var f = new FabricaApi();
        var profesor = f.ClienteProfesor();
        var fecha = DateTimeOffset.UtcNow.AddDays(10);

        var r = await profesor.PostAsJsonAsync($"/cursos/{FabricaApi.Curso}/actividades",
            new { titulo = "Taller 3", corte = 3, peso = 30, fechaLimite = fecha, requiereEntrega = true });
        var lista = (await Json(await profesor.GetAsync($"/cursos/{FabricaApi.Curso}/actividades"))).EnumerateArray().ToList();

        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var creada = await Json(r);
        Assert.Equal($"/actividades/{creada.GetProperty("id").GetString()}", r.Headers.Location?.ToString());
        Assert.EndsWith("Z", creada.GetProperty("fechaLimite").GetString());
        Assert.Contains(lista, a => a.GetProperty("titulo").GetString() == "Taller 3");
    }

    [Fact]
    public async Task CU03_E10_una_actividad_que_pasa_el_corte_de_100_responde_422()
    {
        await using var f = new FabricaApi();

        var r = await f.ClienteProfesor().PostAsJsonAsync($"/cursos/{FabricaApi.Curso}/actividades",
            new { titulo = "Quiz", corte = 1, peso = 10, fechaLimite = (DateTimeOffset?)null, requiereEntrega = false });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, r.StatusCode);
        Assert.Equal("PESOS_ACTIVIDAD_EXCEDIDOS", Codigo(await Json(r)));
    }

    [Fact]
    public async Task CU03_edita_una_actividad()
    {
        await using var f = new FabricaApi();

        var r = await f.ClienteProfesor().PutAsJsonAsync($"/actividades/{FabricaApi.Parcial1}",
            new { titulo = "Parcial 1 presencial", corte = 1, peso = 70, fechaLimite = (DateTimeOffset?)null, requiereEntrega = false });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var editada = await Json(r);
        Assert.Equal("Parcial 1 presencial", editada.GetProperty("titulo").GetString());
        Assert.Equal(70m, editada.GetProperty("peso").GetDecimal());
    }

    [Fact]
    public async Task CU03_con_entrega_y_sin_fecha_responde_400()
    {
        await using var f = new FabricaApi();

        var r = await f.ClienteProfesor().PostAsJsonAsync($"/cursos/{FabricaApi.Curso}/actividades",
            new { titulo = "Informe", corte = 3, peso = 10, requiereEntrega = true });

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal("VALIDACION_FALLIDA", Codigo(await Json(r)));
    }

    [Fact]
    public async Task Interno_sin_clave_de_servicio_responde_401_y_con_clave_devuelve_la_actividad()
    {
        await using var f = new FabricaApi();
        var ruta = $"/internal/actividades/{FabricaApi.Taller1}?estudianteId={FabricaApi.Ana}";
        var sinClave = await f.CreateClient().GetAsync(ruta);
        var conToken = await f.ClienteProfesor().GetAsync(ruta); // el token de usuario no sirve aquí
        var cliente = f.CreateClient();
        cliente.DefaultRequestHeaders.Add("X-Service-Key", FabricaApi.ClaveServicio);

        var conClave = await cliente.GetAsync(ruta);

        Assert.Equal(HttpStatusCode.Unauthorized, sinClave.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, conToken.StatusCode);
        var info = await Json(conClave);
        Assert.Equal(FabricaApi.Profesor.ToString(), info.GetProperty("profesorId").GetString());
        Assert.True(info.GetProperty("requiereEntrega").GetBoolean());
        Assert.True(info.GetProperty("estudianteInscrito").GetBoolean());
    }
}
