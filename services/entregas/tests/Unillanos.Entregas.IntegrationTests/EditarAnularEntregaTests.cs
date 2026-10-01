using System.Net;
using System.Net.Http.Json;
using Unillanos.Entregas.IntegrationTests.Infraestructura;
using Unillanos.Pruebas.Compartidas;
using Unillanos.Pruebas.Compartidas.Contratos;

namespace Unillanos.Entregas.IntegrationTests;

/// <summary>CU-14: editar o anular la entrega propia mientras no venza la fecha.</summary>
public sealed class EditarAnularEntregaTests(EntregasFixture fixture) : PruebaEntregas(fixture)
{
    private const string Ruta = "/entregas/{entregaId}";

    [Fact]
    public async Task Editar_vigente_reemplaza_archivo_y_actualiza_metadatos()
    {
        var entrega = await SubirOkAsync(Ana, UsuariosSemilla.Taller1, "v1.pdf");
        var rutaAnterior = Assert.Single(Api.Almacen.Blobs).Key;
        Api.Reloj.Ahora = Api.Reloj.Ahora.AddHours(1);

        var respuesta = await Ana.PutAsync($"/entregas/{entrega.Id}", Archivo("v2.png", [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0]));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        await respuesta.CumpleContratoAsync(Contrato, "put", Ruta);
        var editada = (await respuesta.Content.ReadFromJsonAsync<EntregaDto>())!;
        Assert.Equal((entrega.Id, "v2.png", 10L, "ENVIADA"), (editada.Id, editada.NombreArchivo, editada.Tamano, editada.Estado));
        Assert.Equal(Api.Reloj.Ahora, editada.FechaEnvio);
        var (rutaNueva, blob) = Assert.Single(Api.Almacen.Blobs);
        Assert.NotEqual(rutaAnterior, rutaNueva);
        Assert.Equal("image/png", blob.ContentType);
    }

    [Fact]
    public async Task Anular_vigente_deja_la_entrega_ANULADA()
    {
        var entrega = await SubirOkAsync(Ana, UsuariosSemilla.Taller1);

        var respuesta = await Ana.DeleteAsync($"/entregas/{entrega.Id}");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        await respuesta.CumpleContratoAsync(Contrato, "delete", Ruta);
        Assert.Equal("ANULADA", (await respuesta.Content.ReadFromJsonAsync<EntregaDto>())!.Estado);
        Assert.Equal("ANULADA", Assert.Single(await FilasAsync()).Estado.ToString());
    }

    [Fact]
    public async Task Volver_a_subir_tras_anular_reutiliza_la_fila_y_vuelve_a_ENVIADA()
    {
        var entrega = await SubirOkAsync(Ana, UsuariosSemilla.Taller1, "v1.pdf");
        await Ana.DeleteAsync($"/entregas/{entrega.Id}");

        var nueva = await SubirOkAsync(Ana, UsuariosSemilla.Taller1, "v2.pdf");

        Assert.Equal(entrega.Id, nueva.Id);
        var fila = Assert.Single(await FilasAsync());
        Assert.Equal(("ENVIADA", "v2.pdf"), (fila.Estado.ToString(), fila.NombreArchivo));
    }

    [Fact]
    public async Task E06_Editar_tras_vencer_responde_FECHA_LIMITE_VENCIDA()
    {
        var entrega = await SubirOkAsync(Ana, UsuariosSemilla.Taller1, "v1.pdf");
        Api.Evaluaciones.Vencer(UsuariosSemilla.Taller1);

        var respuesta = await Ana.PutAsync($"/entregas/{entrega.Id}", Archivo("v2.pdf", Pdf()));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);
        Assert.Equal("FECHA_LIMITE_VENCIDA", (await ErrorDeAsync(respuesta)).Codigo);
        await respuesta.CumpleContratoAsync(Contrato, "put", Ruta);
        Assert.Equal("v1.pdf", Assert.Single(await FilasAsync()).NombreArchivo);
        Assert.Single(Api.Almacen.Blobs);
    }

    [Fact]
    public async Task E06_Anular_tras_vencer_responde_FECHA_LIMITE_VENCIDA()
    {
        var entrega = await SubirOkAsync(Ana, UsuariosSemilla.Taller1);
        Api.Evaluaciones.Vencer(UsuariosSemilla.Taller1);

        var respuesta = await Ana.DeleteAsync($"/entregas/{entrega.Id}");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);
        Assert.Equal("FECHA_LIMITE_VENCIDA", (await ErrorDeAsync(respuesta)).Codigo);
        await respuesta.CumpleContratoAsync(Contrato, "delete", Ruta);
        Assert.Equal("ENVIADA", Assert.Single(await FilasAsync()).Estado.ToString());
    }

    [Fact]
    public async Task Entrega_de_otro_estudiante_responde_404_al_editar_y_al_anular()
    {
        var deAna = await SubirOkAsync(Ana, UsuariosSemilla.Taller1);

        var editar = await Luis.PutAsync($"/entregas/{deAna.Id}", Archivo("x.pdf", Pdf()));
        var anular = await Luis.DeleteAsync($"/entregas/{deAna.Id}");

        Assert.Equal(HttpStatusCode.NotFound, editar.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, anular.StatusCode);
        Assert.Equal("NO_ENCONTRADO", (await ErrorDeAsync(editar)).Codigo);
        await anular.CumpleContratoAsync(Contrato, "delete", Ruta);
        Assert.Equal("ENVIADA", Assert.Single(await FilasAsync()).Estado.ToString());
    }

    [Fact]
    public async Task Entrega_inexistente_responde_404()
    {
        var respuesta = await Ana.DeleteAsync($"/entregas/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }
}
