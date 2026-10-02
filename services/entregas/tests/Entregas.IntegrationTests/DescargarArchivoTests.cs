using System.Net;
using System.Net.Http.Json;
using Entregas.IntegrationTests.Infraestructura;
using Unillanos.Pruebas.Compartidas;
using Unillanos.Pruebas.Compartidas.Contratos;

namespace Entregas.IntegrationTests;

/// <summary>GET /entregas/{entregaId}/archivos/{archivoId}: descarga con el nombre original, sea cual sea el tipo.</summary>
public sealed class DescargarArchivoTests(EntregasFixture fixture) : PruebaEntregas(fixture)
{
    private const string Ruta = "/entregas/{entregaId}/archivos/{archivoId}";

    [Theory]
    [InlineData("Taller 1 - Ana.pdf", "application/pdf")]
    [InlineData("diagrama.png", "image/png")]
    [InlineData("código fuente.zip", "application/zip")]
    public async Task Descarga_el_mismo_contenido_con_su_nombre_y_tipo(string nombre, string contentType)
    {
        byte[] datos = contentType switch
        {
            "image/png" => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 7, 7],
            "application/zip" => [0x50, 0x4B, 0x03, 0x04, 9, 9],
            _ => Pdf(1500),
        };
        var entrega = await SubirOkAsync(Ana, UsuariosSemilla.Taller1, nombre, datos);
        var archivo = Assert.Single(entrega.Archivos);

        var respuesta = await Ana.GetAsync($"/entregas/{entrega.Id}/archivos/{archivo.Id}");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(datos, await respuesta.Content.ReadAsByteArrayAsync());
        Assert.Equal(contentType, respuesta.Content.Headers.ContentType?.MediaType);
        Assert.Equal("attachment", respuesta.Content.Headers.ContentDisposition?.DispositionType);
        Assert.Equal(nombre, respuesta.Content.Headers.ContentDisposition?.FileNameStar);
    }

    [Fact]
    public async Task Se_puede_descargar_aunque_la_fecha_ya_haya_vencido()
    {
        var entrega = await SubirOkAsync(Ana, UsuariosSemilla.Taller1);
        Api.Evaluaciones.Vencer(UsuariosSemilla.Taller1);

        var respuesta = await Ana.GetAsync($"/entregas/{entrega.Id}/archivos/{entrega.Archivos[0].Id}");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact]
    public async Task Archivo_de_otro_estudiante_responde_404()
    {
        var deAna = await SubirOkAsync(Ana, UsuariosSemilla.Taller1);

        var respuesta = await Luis.GetAsync($"/entregas/{deAna.Id}/archivos/{deAna.Archivos[0].Id}");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Equal("NO_ENCONTRADO", (await ErrorDeAsync(respuesta)).Codigo);
        await respuesta.CumpleContratoAsync(Contrato, "get", Ruta);
    }

    [Fact]
    public async Task Archivo_que_no_es_de_esa_entrega_responde_404()
    {
        var entrega = await SubirOkAsync(Ana, UsuariosSemilla.Taller1);

        var respuesta = await Ana.GetAsync($"/entregas/{entrega.Id}/archivos/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Fact]
    public async Task Sin_token_responde_401()
    {
        var respuesta = await Api.CreateClient().GetAsync($"/entregas/{Guid.NewGuid()}/archivos/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        await respuesta.CumpleContratoAsync(Contrato, "get", Ruta);
    }
}
