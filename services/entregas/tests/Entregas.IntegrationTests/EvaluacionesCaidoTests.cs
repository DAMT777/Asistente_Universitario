using System.Net;
using Entregas.IntegrationTests.Infraestructura;
using Unillanos.Pruebas.Compartidas;
using Unillanos.Pruebas.Compartidas.Contratos;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Settings;

namespace Entregas.IntegrationTests;

/// <summary>
/// E-16: cliente HTTP real (timeout configurado de 3 s, sin reintentos) contra Evaluaciones simulado con WireMock.
/// </summary>
public sealed class EvaluacionesCaidoTests(EntregasFixture fixture) : PruebaEntregas(fixture)
{
    private const string Ruta = "/actividades/{actividadId}/entregas";
    private string RutaInterna => $"/internal/actividades/{UsuariosSemilla.Taller1}";

    protected override EntregasApiFactory Api => Fixture.ApiConWireMock;

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        Fixture.WireMock.Reset();
    }

    [Fact]
    public async Task Con_Evaluaciones_respondiendo_usa_X_Service_Key_y_propaga_X_Correlation_Id()
    {
        Fixture.WireMock
            .Given(Request.Create().WithPath(RutaInterna).WithParam("estudianteId", UsuariosSemilla.Ana.ToString()).UsingGet()
                .WithHeader("X-Service-Key", EntregasApiFactory.ClaveServicio))
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new
            {
                actividadId = UsuariosSemilla.Taller1,
                cursoId = UsuariosSemilla.Curso,
                profesorId = UsuariosSemilla.Profesor,
                fechaLimite = Api.Reloj.Ahora.AddDays(30).UtcDateTime.ToString("O"),
                requiereEntrega = true,
                estudianteInscrito = true,
            }));
        var cliente = Ana;
        cliente.DefaultRequestHeaders.Add("X-Correlation-Id", "corr-e16");

        var respuesta = await SubirAsync(cliente, UsuariosSemilla.Taller1, "t.pdf", Pdf());

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var llamada = Assert.Single(Fixture.WireMock.LogEntries);
        Assert.Equal("corr-e16", llamada.RequestMessage!.Headers!["X-Correlation-Id"].Single());
    }

    [Fact]
    public async Task E16_Timeout_de_Evaluaciones_responde_503_SERVICIO_NO_DISPONIBLE()
    {
        Fixture.WireMock
            .Given(Request.Create().WithPath(RutaInterna).UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody("{}").WithDelay(TimeSpan.FromSeconds(4)));

        var inicio = TimeProvider.System.GetTimestamp();
        var respuesta = await SubirAsync(Ana, UsuariosSemilla.Taller1, "t.pdf", Pdf());
        var duracion = TimeProvider.System.GetElapsedTime(inicio);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, respuesta.StatusCode);
        Assert.Equal("SERVICIO_NO_DISPONIBLE", (await ErrorDeAsync(respuesta)).Codigo);
        await respuesta.CumpleContratoAsync(Contrato, "post", Ruta);
        Assert.InRange(duracion, TimeSpan.FromSeconds(2.5), TimeSpan.FromSeconds(3.9));
        // WireMock registra la llamada cuando termina su demora: se espera a que pase y se verifica que no hubo reintentos.
        await Task.Delay(TimeSpan.FromSeconds(2));
        Assert.Single(Fixture.WireMock.LogEntries);
        Assert.Empty(await FilasAsync());
    }

    [Fact]
    public async Task E16_Evaluaciones_caido_responde_503_SERVICIO_NO_DISPONIBLE()
    {
        Fixture.WireMock
            .Given(Request.Create().WithPath(RutaInterna).UsingGet())
            .RespondWith(Response.Create().WithFault(FaultType.EMPTY_RESPONSE));

        var respuesta = await SubirAsync(Ana, UsuariosSemilla.Taller1, "t.pdf", Pdf());

        Assert.Equal(HttpStatusCode.ServiceUnavailable, respuesta.StatusCode);
        Assert.Equal("SERVICIO_NO_DISPONIBLE", (await ErrorDeAsync(respuesta)).Codigo);
        Assert.Empty(Api.Almacen.Blobs);
    }

    [Fact]
    public async Task E16_Error_500_de_Evaluaciones_responde_503()
    {
        Fixture.WireMock
            .Given(Request.Create().WithPath(RutaInterna).UsingGet())
            .RespondWith(Response.Create().WithStatusCode(500));

        var respuesta = await SubirAsync(Ana, UsuariosSemilla.Taller1, "t.pdf", Pdf());

        Assert.Equal(HttpStatusCode.ServiceUnavailable, respuesta.StatusCode);
        Assert.Single(Fixture.WireMock.LogEntries);
    }

    [Fact]
    public async Task Actividad_inexistente_en_Evaluaciones_responde_404()
    {
        Fixture.WireMock
            .Given(Request.Create().WithPath(RutaInterna).UsingGet())
            .RespondWith(Response.Create().WithStatusCode(404));

        var respuesta = await SubirAsync(Ana, UsuariosSemilla.Taller1, "t.pdf", Pdf());

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Equal("NO_ENCONTRADO", (await ErrorDeAsync(respuesta)).Codigo);
    }
}
