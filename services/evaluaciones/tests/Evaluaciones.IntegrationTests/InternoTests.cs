using System.Net;
using System.Net.Http.Json;
using Evaluaciones.IntegrationTests.Infraestructura;
using Unillanos.Pruebas.Compartidas;
using Unillanos.Pruebas.Compartidas.Contratos;

namespace Evaluaciones.IntegrationTests;

/// <summary>GET /internal/actividades/{id}: solo con X-Service-Key.</summary>
[Collection(ColeccionEvaluaciones.Nombre)]
public sealed class InternoTests(EvaluacionesFixture fixture)
{
    private const string Ruta = "/internal/actividades/{actividadId}";
    private static readonly ContratoOpenApi Contrato = ContratoOpenApi.Cargar("evaluaciones.yaml");

    private HttpClient ConClave(string clave = EvaluacionesFixture.ClaveServicio)
    {
        var cliente = fixture.Api.CreateClient();
        cliente.DefaultRequestHeaders.Add("X-Service-Key", clave);
        return cliente;
    }

    [Fact]
    public async Task Con_clave_devuelve_los_datos_de_la_actividad()
    {
        var respuesta = await ConClave().GetAsync($"/internal/actividades/{UsuariosSemilla.Taller1}?estudianteId={UsuariosSemilla.Ana}");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        await respuesta.CumpleContratoAsync(Contrato, "get", Ruta);
        var a = (await respuesta.Content.ReadFromJsonAsync<ActividadInternaDto>())!;
        Assert.Equal((UsuariosSemilla.Taller1, UsuariosSemilla.Curso, UsuariosSemilla.Profesor, true, true),
            (a.ActividadId, a.CursoId, a.ProfesorId, a.RequiereEntrega, a.EstudianteInscrito));
        Assert.Equal(TimeSpan.Zero, a.FechaLimite.Offset);
        Assert.True(a.FechaLimite > DateTimeOffset.UtcNow);
        Assert.EndsWith("Z\",\"requiereEntrega\":true,\"estudianteInscrito\":true}", await respuesta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Informa_si_el_estudiante_no_esta_inscrito_y_las_actividades_vencidas_o_sin_entrega()
    {
        var pedro = await ConClave().GetFromJsonAsync<ActividadInternaDto>($"/internal/actividades/{UsuariosSemilla.Proyecto1}?estudianteId={UsuariosSemilla.Pedro}");
        var parcial = await ConClave().GetFromJsonAsync<ActividadInternaDto>($"/internal/actividades/{UsuariosSemilla.Parcial1}?estudianteId={UsuariosSemilla.Ana}");

        Assert.False(pedro!.EstudianteInscrito);
        Assert.True(pedro.FechaLimite < DateTimeOffset.UtcNow);
        Assert.False(parcial!.RequiereEntrega);
    }

    [Fact]
    public async Task Actividad_inexistente_responde_404()
    {
        var respuesta = await ConClave().GetAsync($"/internal/actividades/{Guid.NewGuid()}?estudianteId={UsuariosSemilla.Ana}");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        await respuesta.CumpleContratoAsync(Contrato, "get", Ruta);
    }

    [Fact]
    public async Task Sin_estudianteId_responde_400()
    {
        var respuesta = await ConClave().GetAsync($"/internal/actividades/{UsuariosSemilla.Taller1}");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        await respuesta.CumpleContratoAsync(Contrato, "get", Ruta);
    }

    [Fact]
    public async Task Sin_clave_o_con_clave_incorrecta_responde_401_aunque_traiga_JWT()
    {
        var url = $"/internal/actividades/{UsuariosSemilla.Taller1}?estudianteId={UsuariosSemilla.Ana}";

        var sinClave = await fixture.ClienteDe(UsuariosSemilla.Ana).GetAsync(url);
        var claveMala = await ConClave("clave-incorrecta-de-servicio").GetAsync(url);

        Assert.Equal(HttpStatusCode.Unauthorized, sinClave.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, claveMala.StatusCode);
        Assert.Equal("NO_AUTENTICADO", (await sinClave.Content.ReadFromJsonAsync<ErrorDto>())!.Codigo);
        await claveMala.CumpleContratoAsync(Contrato, "get", Ruta);
    }
}
