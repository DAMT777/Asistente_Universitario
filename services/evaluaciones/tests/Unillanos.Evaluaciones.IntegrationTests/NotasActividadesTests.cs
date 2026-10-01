using System.Net;
using System.Net.Http.Json;
using Unillanos.Evaluaciones.IntegrationTests.Infraestructura;
using Unillanos.Pruebas.Compartidas;
using Unillanos.Pruebas.Compartidas.Contratos;

namespace Unillanos.Evaluaciones.IntegrationTests;

/// <summary>CU-15: nota y retroalimentación por actividad.</summary>
[Collection(ColeccionEvaluaciones.Nombre)]
public sealed class NotasActividadesTests(EvaluacionesFixture fixture)
{
    private const string Ruta = "/mis-notas/cursos/{cursoId}/actividades";
    private static readonly ContratoOpenApi Contrato = ContratoOpenApi.Cargar("evaluaciones.yaml");
    private static readonly string Url = $"/mis-notas/cursos/{UsuariosSemilla.Curso}/actividades";

    [Fact]
    public async Task Ana_ve_sus_notas_publicadas_con_retroalimentacion()
    {
        var respuesta = await fixture.ClienteDe(UsuariosSemilla.Ana).GetAsync(Url);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        await respuesta.CumpleContratoAsync(Contrato, "get", Ruta);
        var r = (await respuesta.Content.ReadFromJsonAsync<NotasActividadesDto>())!;
        Assert.Equal(UsuariosSemilla.Curso, r.CursoId);
        var taller = r.Actividades.Single(a => a.ActividadId == UsuariosSemilla.Taller1);
        var parcial = r.Actividades.Single(a => a.ActividadId == UsuariosSemilla.Parcial1);
        var proyecto = r.Actividades.Single(a => a.ActividadId == UsuariosSemilla.Proyecto1);
        Assert.Equal(("PUBLICADA", 4.0m), (taller.Estado, taller.Nota));
        Assert.False(string.IsNullOrEmpty(taller.Retroalimentacion));
        Assert.Equal(("PUBLICADA", 3.0m), (parcial.Estado, parcial.Nota));
        Assert.Equal(("SIN_CALIFICAR", (decimal?)null, (string?)null), (proyecto.Estado, proyecto.Nota, proyecto.Retroalimentacion));
        Assert.Equal((1, 20m), (taller.Corte, taller.Peso));
    }

    [Fact]
    public async Task E07_Luis_ve_su_borrador_de_Taller_1_como_SIN_CALIFICAR_sin_el_valor()
    {
        var respuesta = await fixture.ClienteDe(UsuariosSemilla.Luis).GetAsync(Url);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        await respuesta.CumpleContratoAsync(Contrato, "get", Ruta);
        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        Assert.DoesNotContain("2.5", cuerpo);
        Assert.DoesNotContain("Borrador", cuerpo, StringComparison.OrdinalIgnoreCase);
        var taller = (await respuesta.Content.ReadFromJsonAsync<NotasActividadesDto>())!.Actividades.Single(a => a.ActividadId == UsuariosSemilla.Taller1);
        Assert.Equal("SIN_CALIFICAR", taller.Estado);
        Assert.Null(taller.Nota);
        Assert.Null(taller.Retroalimentacion);
    }

    [Fact]
    public async Task Marta_sin_calificaciones_ve_todo_SIN_CALIFICAR_y_no_cero()
    {
        var r = await fixture.ClienteDe(UsuariosSemilla.Marta).GetFromJsonAsync<NotasActividadesDto>(Url);

        Assert.Equal(3, r!.Actividades.Count);
        Assert.All(r.Actividades, a => Assert.Equal(("SIN_CALIFICAR", (decimal?)null), (a.Estado, a.Nota)));
    }

    [Fact]
    public async Task Estudiante_no_inscrito_recibe_403_SIN_PERMISO()
    {
        var respuesta = await fixture.ClienteDe(UsuariosSemilla.Pedro).GetAsync(Url);

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Equal("SIN_PERMISO", (await respuesta.Content.ReadFromJsonAsync<ErrorDto>())!.Codigo);
        await respuesta.CumpleContratoAsync(Contrato, "get", Ruta);
    }

    [Fact]
    public async Task Curso_inexistente_responde_404()
    {
        var respuesta = await fixture.ClienteDe(UsuariosSemilla.Ana).GetAsync($"/mis-notas/cursos/{Guid.NewGuid()}/actividades");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Equal("NO_ENCONTRADO", (await respuesta.Content.ReadFromJsonAsync<ErrorDto>())!.Codigo);
        await respuesta.CumpleContratoAsync(Contrato, "get", Ruta);
    }
}
