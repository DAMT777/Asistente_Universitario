using System.Net;
using System.Net.Http.Json;
using Entregas.IntegrationTests.Infraestructura;
using Unillanos.Pruebas.Compartidas;
using Unillanos.Pruebas.Compartidas.Contratos;

namespace Entregas.IntegrationTests;

/// <summary>Lo que necesita la pantalla de calificar: ver y descargar las entregas de la actividad (RN-15).</summary>
public sealed class ProfesorEntregasTests(EntregasFixture fixture) : PruebaEntregas(fixture)
{
    private const string Ruta = "/actividades/{actividadId}/entregas";
    private HttpClient Profesor => ClienteDe(UsuariosSemilla.Profesor, rol: "PROFESOR", nombre: "Profesor Demo");
    private HttpClient OtroProfesor => ClienteDe(Guid.NewGuid(), rol: "PROFESOR", nombre: "Otro");

    [Fact]
    public async Task El_profesor_dueno_ve_las_entregas_de_la_actividad()
    {
        await SubirOkAsync(Ana, UsuariosSemilla.Taller1, "ana.pdf");
        await SubirOkAsync(Luis, UsuariosSemilla.Taller1, "luis.pdf");

        var respuesta = await Profesor.GetAsync($"/actividades/{UsuariosSemilla.Taller1}/entregas");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        await respuesta.CumpleContratoAsync(Contrato, "get", Ruta);
        var entregas = (await respuesta.Content.ReadFromJsonAsync<List<EntregaDto>>())!;
        Assert.Equal([UsuariosSemilla.Ana, UsuariosSemilla.Luis], entregas.Select(e => e.EstudianteId).Order());
    }

    [Fact]
    public async Task Otro_profesor_recibe_403_y_un_estudiante_tambien()
    {
        var otro = await OtroProfesor.GetAsync($"/actividades/{UsuariosSemilla.Taller1}/entregas");
        var estudiante = await Ana.GetAsync($"/actividades/{UsuariosSemilla.Taller1}/entregas");

        Assert.Equal(HttpStatusCode.Forbidden, otro.StatusCode);
        Assert.Equal("SIN_PERMISO", (await ErrorDeAsync(otro)).Codigo);
        await otro.CumpleContratoAsync(Contrato, "get", Ruta);
        Assert.Equal(HttpStatusCode.Forbidden, estudiante.StatusCode);
    }

    [Fact]
    public async Task El_profesor_dueno_descarga_el_archivo_de_un_estudiante_y_otro_profesor_no()
    {
        var entrega = await SubirOkAsync(Ana, UsuariosSemilla.Taller1, "ana.pdf", Pdf(1200));
        var url = $"/entregas/{entrega.Id}/archivos/{entrega.Archivos[0].Id}";

        var dueno = await Profesor.GetAsync(url);
        var ajeno = await OtroProfesor.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, dueno.StatusCode);
        Assert.Equal(1200, (await dueno.Content.ReadAsByteArrayAsync()).Length);
        Assert.Equal(HttpStatusCode.Forbidden, ajeno.StatusCode);
    }

    [Fact]
    public async Task Una_actividad_sin_fecha_limite_recibe_entregas()
    {
        var respuesta = await SubirAsync(Ana, EvaluacionesFalso.SinFecha, "trabajo.pdf", Pdf());

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
    }
}
