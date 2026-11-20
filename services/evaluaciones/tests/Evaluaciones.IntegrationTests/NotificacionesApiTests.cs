using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Unillanos.Pruebas.Compartidas.Contratos;

namespace Evaluaciones.IntegrationTests;

/// <summary>Notificaciones de punta a punta: el profesor actúa, el estudiante ve el aviso, y la respuesta cumple el contrato.</summary>
public class NotificacionesApiTests
{
    private static readonly ContratoOpenApi Contrato = ContratoOpenApi.Cargar("evaluaciones.yaml");

    private static async Task<JsonElement> Json(HttpResponseMessage r) => await r.Content.ReadFromJsonAsync<JsonElement>();

    [Fact]
    public async Task Sin_avisos_responde_lista_vacia_y_cero_no_leidas()
    {
        await using var f = new FabricaApi();

        var r = await f.ClienteEstudiante(FabricaApi.Marta).GetAsync("/notificaciones");

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        await r.CumpleContratoAsync(Contrato, "get", "/notificaciones");
        var cuerpo = await Json(r);
        Assert.Equal(0, cuerpo.GetProperty("noLeidas").GetInt32());
        Assert.Equal(0, cuerpo.GetProperty("notificaciones").GetArrayLength());
    }

    [Fact]
    public async Task Una_actividad_nueva_llega_al_estudiante_y_se_marca_leida()
    {
        await using var f = new FabricaApi();
        var marta = f.ClienteEstudiante(FabricaApi.Marta);

        var creada = await f.ClienteProfesor().PostAsJsonAsync($"/cursos/{FabricaApi.Curso}/actividades",
            new { titulo = "Taller 3", corte = 3, peso = 30, fechaLimite = DateTimeOffset.UtcNow.AddDays(10), requiereEntrega = true });
        Assert.Equal(HttpStatusCode.Created, creada.StatusCode);

        var r = await marta.GetAsync("/notificaciones");
        await r.CumpleContratoAsync(Contrato, "get", "/notificaciones");
        var cuerpo = await Json(r);
        var aviso = cuerpo.GetProperty("notificaciones")[0];
        Assert.Equal(1, cuerpo.GetProperty("noLeidas").GetInt32());
        Assert.Equal("NUEVA_ACTIVIDAD", aviso.GetProperty("tipo").GetString());
        Assert.Equal((await Json(creada)).GetProperty("id").GetString(), aviso.GetProperty("actividadId").GetString());

        var id = aviso.GetProperty("id").GetString();
        var ajena = await f.ClienteEstudiante(FabricaApi.Ana).PostAsync($"/notificaciones/{id}/leida", null);
        var propia = await marta.PostAsync($"/notificaciones/{id}/leida", null);

        Assert.Equal(HttpStatusCode.NotFound, ajena.StatusCode);
        await ajena.CumpleContratoAsync(Contrato, "post", "/notificaciones/{notificacionId}/leida");
        Assert.Equal(HttpStatusCode.NoContent, propia.StatusCode);
        Assert.Equal(0, (await Json(await marta.GetAsync("/notificaciones"))).GetProperty("noLeidas").GetInt32());
    }

    [Fact]
    public async Task Publicar_notas_de_la_actividad_avisa_y_marcar_todas_las_deja_leidas()
    {
        await using var f = new FabricaApi();
        var luis = f.ClienteEstudiante(FabricaApi.Luis);

        // En la semilla Luis tiene un borrador en Taller 1: no debe haber aviso hasta que se publique.
        Assert.Equal(0, (await Json(await luis.GetAsync("/notificaciones"))).GetProperty("noLeidas").GetInt32());

        await f.ClienteProfesor().PostAsync($"/actividades/{FabricaApi.Taller1}/calificaciones/publicar", null);

        var bandeja = await Json(await luis.GetAsync("/notificaciones"));
        Assert.Equal("NOTA_PUBLICADA", bandeja.GetProperty("notificaciones")[0].GetProperty("tipo").GetString());

        var todas = await luis.PostAsync("/notificaciones/leidas", null);
        Assert.Equal(HttpStatusCode.OK, todas.StatusCode);
        await todas.CumpleContratoAsync(Contrato, "post", "/notificaciones/leidas");
        Assert.Equal(0, (await Json(await luis.GetAsync("/notificaciones"))).GetProperty("noLeidas").GetInt32());
    }

    [Fact]
    public async Task Sin_token_responde_401()
    {
        await using var f = new FabricaApi();

        var r = await f.CreateClient().GetAsync("/notificaciones");

        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }
}
