using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using Entregas.IntegrationTests.Infraestructura;
using Unillanos.Pruebas.Compartidas;
using Unillanos.Pruebas.Compartidas.Contratos;

namespace Entregas.IntegrationTests;

public sealed class SeguridadTests(EntregasFixture fixture) : PruebaEntregas(fixture)
{
    [Fact]
    public async Task Sin_token_responde_401_NO_AUTENTICADO()
    {
        var respuesta = await Api.CreateClient().PostAsync($"/actividades/{UsuariosSemilla.Taller1}/entregas", Archivo("t.pdf", Pdf()));

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Equal("NO_AUTENTICADO", (await ErrorDeAsync(respuesta)).Codigo);
        await respuesta.CumpleContratoAsync(Contrato, "post", "/actividades/{actividadId}/entregas");
    }

    [Fact]
    public async Task Token_expirado_responde_401_TOKEN_EXPIRADO()
    {
        var respuesta = await ClienteDe(UsuariosSemilla.Ana, expirado: true).GetAsync("/mis-entregas");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Equal("TOKEN_EXPIRADO", (await ErrorDeAsync(respuesta)).Codigo);
        await respuesta.CumpleContratoAsync(Contrato, "get", "/mis-entregas");
    }

    [Fact]
    public async Task Token_firmado_con_otra_llave_responde_401()
    {
        using var otra = RSA.Create(2048);
        var cliente = Api.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            Fixture.Llaves.CrearToken(UsuariosSemilla.Ana, "ESTUDIANTE", "Ana", firmarCon: otra));

        var respuesta = await cliente.GetAsync("/mis-entregas");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Equal("NO_AUTENTICADO", (await ErrorDeAsync(respuesta)).Codigo);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    [InlineData("GET")]
    public async Task Rol_PROFESOR_en_endpoints_de_estudiante_responde_403(string metodo)
    {
        var profesor = ClienteDe(UsuariosSemilla.Profesor, rol: "PROFESOR", nombre: "Laura");
        var solicitud = metodo switch
        {
            "POST" => new HttpRequestMessage(HttpMethod.Post, $"/actividades/{UsuariosSemilla.Taller1}/entregas") { Content = Archivo("t.pdf", Pdf()) },
            "PUT" => new HttpRequestMessage(HttpMethod.Put, $"/entregas/{Guid.NewGuid()}") { Content = Archivo("t.pdf", Pdf()) },
            "DELETE" => new HttpRequestMessage(HttpMethod.Delete, $"/entregas/{Guid.NewGuid()}"),
            _ => new HttpRequestMessage(HttpMethod.Get, "/mis-entregas"),
        };

        var respuesta = await profesor.SendAsync(solicitud);

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Equal("SIN_PERMISO", (await ErrorDeAsync(respuesta)).Codigo);
    }

    [Fact]
    public async Task El_X_Correlation_Id_se_devuelve_y_es_el_traceId_del_error()
    {
        var cliente = Api.CreateClient();
        cliente.DefaultRequestHeaders.Add("X-Correlation-Id", "prueba-correlacion-123");

        var respuesta = await cliente.GetAsync("/mis-entregas");

        Assert.Equal("prueba-correlacion-123", respuesta.Headers.GetValues("X-Correlation-Id").Single());
        Assert.Equal("prueba-correlacion-123", (await ErrorDeAsync(respuesta)).TraceId);
    }

    [Fact]
    public async Task Health_live_y_ready_no_requieren_token()
    {
        var cliente = Api.CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/health/live")).StatusCode);
        // ready depende de Blob Storage real, que aquí es un doble: solo se verifica que no exige token.
        Assert.NotEqual(HttpStatusCode.Unauthorized, (await cliente.GetAsync("/health/ready")).StatusCode);
    }

    [Fact]
    public async Task Ruta_desconocida_responde_404_con_formato_estandar()
    {
        var respuesta = await Ana.GetAsync("/no-existe");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Equal("NO_ENCONTRADO", (await ErrorDeAsync(respuesta)).Codigo);
    }
}
