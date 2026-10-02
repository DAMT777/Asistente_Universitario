using System.Net;
using System.Net.Http.Json;
using Entregas.IntegrationTests.Infraestructura;
using Unillanos.Pruebas.Compartidas;
using Unillanos.Pruebas.Compartidas.Contratos;

namespace Entregas.IntegrationTests;

/// <summary>Entregas con varios archivos, límites de cantidad y tamaño total.</summary>
public sealed class VariosArchivosTests(EntregasFixture fixture) : PruebaEntregas(fixture)
{
    private const string Ruta = "/actividades/{actividadId}/entregas";
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3];
    private static readonly byte[] Zip = [0x50, 0x4B, 0x03, 0x04, 0x14, 0x00, 0, 0];

    private Task<HttpResponseMessage> SubirVariosAsync(params (string, byte[])[] archivos)
        => Ana.PostAsync($"/actividades/{UsuariosSemilla.Taller1}/entregas", Archivos(archivos));

    [Fact]
    public async Task Sube_varios_archivos_en_una_sola_entrega()
    {
        var respuesta = await SubirVariosAsync(("informe.pdf", Pdf(3000)), ("diagrama.png", Png), ("codigo.zip", Zip));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        await respuesta.CumpleContratoAsync(Contrato, "post", Ruta);
        var entrega = (await respuesta.Content.ReadFromJsonAsync<EntregaDto>())!;
        Assert.Equal(["informe.pdf", "diagrama.png", "codigo.zip"], entrega.Archivos.Select(a => a.NombreArchivo));
        Assert.Equal(3000 + Png.Length + Zip.Length, entrega.TamanoTotal);

        var fila = Assert.Single(await FilasAsync());
        Assert.Equal(3, fila.Archivos.Count);
        Assert.Equal(3, Api.Almacen.Blobs.Count);
        Assert.Equal(["application/pdf", "image/png", "application/zip"], fila.Archivos.Select(a => Api.Almacen.Blobs[a.RutaBlob].ContentType));
    }

    [Fact]
    public async Task Mas_archivos_que_el_maximo_responde_400()
    {
        var respuesta = await SubirVariosAsync(("a.pdf", Pdf()), ("b.pdf", Pdf()), ("c.pdf", Pdf()), ("d.pdf", Pdf()));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("VALIDACION_FALLIDA", (await ErrorDeAsync(respuesta)).Codigo);
        await respuesta.CumpleContratoAsync(Contrato, "post", Ruta);
        Assert.Empty(Api.Almacen.Blobs);
    }

    [Fact]
    public async Task Si_la_suma_supera_el_total_responde_413_aunque_cada_archivo_quepa()
    {
        var casiMaximo = (int)EntregasApiFactory.MaxBytesPrueba - 10;

        var respuesta = await SubirVariosAsync(("a.pdf", Pdf(casiMaximo)), ("b.pdf", Pdf(casiMaximo)), ("c.pdf", Pdf(casiMaximo)));

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, respuesta.StatusCode);
        Assert.Equal("ARCHIVO_DEMASIADO_GRANDE", (await ErrorDeAsync(respuesta)).Codigo);
        Assert.Empty(await FilasAsync());
    }

    [Fact]
    public async Task Si_un_archivo_es_invalido_no_se_guarda_ninguno()
    {
        byte[] exe = [0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00];

        var respuesta = await SubirVariosAsync(("bueno.pdf", Pdf()), ("malo.pdf", exe));

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, respuesta.StatusCode);
        Assert.Contains("malo.pdf", (await ErrorDeAsync(respuesta)).Mensaje);
        Assert.Empty(Api.Almacen.Blobs);
        Assert.Empty(await FilasAsync());
    }

    [Fact]
    public async Task Editar_con_varios_archivos_reemplaza_el_conjunto_y_borra_los_blobs_viejos()
    {
        var entrega = (await (await SubirVariosAsync(("v1.pdf", Pdf()), ("v1.png", Png))).Content.ReadFromJsonAsync<EntregaDto>())!;
        var rutasViejas = Api.Almacen.Blobs.Keys.ToHashSet();

        var respuesta = await Ana.PutAsync($"/entregas/{entrega.Id}", Archivos(("v2.pdf", Pdf()), ("v2.zip", Zip), ("v2.png", Png)));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        await respuesta.CumpleContratoAsync(Contrato, "put", "/entregas/{entregaId}");
        var fila = Assert.Single(await FilasAsync());
        Assert.Equal(["v2.pdf", "v2.png", "v2.zip"], fila.Archivos.Select(a => a.NombreArchivo).Order());
        Assert.Equal(3, Api.Almacen.Blobs.Count);
        Assert.DoesNotContain(Api.Almacen.Blobs.Keys, rutasViejas.Contains);
    }
}
