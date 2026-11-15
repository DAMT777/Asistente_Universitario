using System.Net;
using System.Net.Http.Json;
using Asistente.IntegrationTests.Infraestructura;
using Unillanos.Pruebas.Compartidas;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Asistente.IntegrationTests;

public sealed class SeguridadTests(AsistenteFixture fixture) : PruebaAsistente(fixture)
{
    [Fact]
    public async Task Sin_token_da_401()
    {
        var r = await EnviarAsync(Cliente(null), "hola");
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
        Assert.Equal("NO_AUTENTICADO", (await ErrorAsync(r)).Codigo);
    }

    [Fact]
    public async Task Token_expirado_da_401_token_expirado()
    {
        var r = await EnviarAsync(Cliente(TokenDe(UsuariosSemilla.Ana, "ESTUDIANTE", expirado: true)), "hola");
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
        Assert.Equal("TOKEN_EXPIRADO", (await ErrorAsync(r)).Codigo);
    }

    [Fact]
    public async Task Mensaje_vacio_da_400()
    {
        var r = await EnviarAsync(Cliente(TokenAna), "  ");
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal("VALIDACION_FALLIDA", (await ErrorAsync(r)).Codigo);
    }

    [Fact]
    public async Task Limita_las_llamadas_a_herramientas_por_turno()
    {
        for (var i = 0; i < 8; i++) Llm.Llamar("consultar_mis_notas");
        Llm.Responder("Fin.");

        await LeerAsync(await EnviarAsync(Cliente(TokenAna), "repite"));

        Assert.Equal(5, Gateway.LogEntries.Count(e => e.RequestMessage!.Path! == "/mis-notas"));
        Assert.Empty(Llm.Recibido[^1].Herramientas);
    }
}

/// <summary>El cliente real del proveedor (HTTP compatible con OpenAI) contra WireMock.</summary>
public sealed class ProveedorLlmTests(AsistenteFixture fixture) : PruebaAsistente(fixture)
{
    protected override AsistenteApiFactory Api => Fixture.ApiConGroq;

    private void Groq(IResponseBuilder respuesta) =>
        Fixture.Groq.Given(Request.Create().WithPath("/openai/v1/chat/completions").UsingPost()).RespondWith(respuesta);

    [Fact]
    public async Task Groq_429_da_503_asistente_no_disponible()
    {
        Groq(Response.Create().WithStatusCode(429));
        var r = await EnviarAsync(Cliente(TokenAna), "hola");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, r.StatusCode);
        Assert.Equal("ASISTENTE_NO_DISPONIBLE", (await ErrorAsync(r)).Codigo);
        Assert.Single(Fixture.Groq.LogEntries);
    }

    [Fact]
    public async Task Groq_con_timeout_da_503_asistente_no_disponible()
    {
        Groq(Response.Create().WithStatusCode(200).WithDelay(TimeSpan.FromSeconds(3)).WithBodyAsJson(new { choices = Array.Empty<object>() }));
        var r = await EnviarAsync(Cliente(TokenAna), "hola");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, r.StatusCode);
        Assert.Equal("ASISTENTE_NO_DISPONIBLE", (await ErrorAsync(r)).Codigo);
    }

    [Fact]
    public async Task Envia_herramientas_de_su_rol_y_lee_la_respuesta()
    {
        Groq(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json")
            .WithBody("""{"choices":[{"message":{"role":"assistant","content":"Hola Ana"}}]}"""));

        var r = await LeerAsync(await EnviarAsync(Cliente(TokenAna), "hola"));

        Assert.Equal("Hola Ana", r.Respuesta);
        var solicitud = Assert.Single(Fixture.Groq.LogEntries).RequestMessage!;
        Assert.Equal("Bearer llave-falsa-de-pruebas", solicitud.Headers!["Authorization"].Single());
        Assert.Contains("\"model\":\"modelo-de-pruebas\"", solicitud.Body!);
        Assert.Contains("\"name\":\"consultar_mis_notas\"", solicitud.Body!);
        Assert.DoesNotContain("\"name\":\"proponer_calificacion\"", solicitud.Body!);
    }
}
