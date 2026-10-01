using System.Net;
using Entregas.IntegrationTests.Infraestructura;
using Unillanos.Pruebas.Compartidas;
using Unillanos.Pruebas.Compartidas.Contratos;

namespace Entregas.IntegrationTests;

/// <summary>CU-13: subir una entrega.</summary>
public sealed class SubirEntregaTests(EntregasFixture fixture) : PruebaEntregas(fixture)
{
    private const string Ruta = "/actividades/{actividadId}/entregas";

    [Fact]
    public async Task E03_Ana_sube_a_Taller_1_abierta_y_queda_ENVIADA()
    {
        var respuesta = await SubirAsync(Ana, UsuariosSemilla.Taller1, "Taller 1 - Ana.pdf", Pdf());

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        await respuesta.CumpleContratoAsync(Contrato, "post", Ruta);

        var fila = Assert.Single(await FilasAsync());
        Assert.Equal(("ENVIADA", UsuariosSemilla.Ana, UsuariosSemilla.Taller1), (fila.Estado.ToString(), fila.EstudianteId, fila.ActividadId));
        Assert.Equal("Taller 1 - Ana.pdf", fila.NombreArchivo);
        Assert.Equal(2048, fila.Tamano);
        Assert.Equal(Api.Reloj.Ahora.UtcDateTime, fila.FechaEnvio);

        // El blob usa una ruta basada en GUID, nunca el nombre original.
        var (ruta, blob) = Assert.Single(Api.Almacen.Blobs);
        Assert.Equal(ruta, fila.RutaBlob);
        Assert.Matches($"^{UsuariosSemilla.Taller1}/{UsuariosSemilla.Ana}/[0-9a-f-]{{36}}$", ruta);
        Assert.Equal("application/pdf", blob.ContentType);
        Assert.Equal(2048, blob.Datos.Length);
    }

    [Fact]
    public async Task E04_Ana_sube_a_Proyecto_1_vencido_y_recibe_FECHA_LIMITE_VENCIDA()
    {
        var respuesta = await SubirAsync(Ana, UsuariosSemilla.Proyecto1, "proyecto.pdf", Pdf());

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);
        Assert.Equal("FECHA_LIMITE_VENCIDA", (await ErrorDeAsync(respuesta)).Codigo);
        await respuesta.CumpleContratoAsync(Contrato, "post", Ruta);
        Assert.Empty(await FilasAsync());
        Assert.Empty(Api.Almacen.Blobs);
    }

    [Fact]
    public async Task E05_Subir_a_Parcial_1_sin_entrega_responde_ACTIVIDAD_SIN_ENTREGA()
    {
        var respuesta = await SubirAsync(Ana, UsuariosSemilla.Parcial1, "parcial.pdf", Pdf());

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);
        Assert.Equal("ACTIVIDAD_SIN_ENTREGA", (await ErrorDeAsync(respuesta)).Codigo);
        await respuesta.CumpleContratoAsync(Contrato, "post", Ruta);
    }

    [Fact]
    public async Task Archivo_mayor_al_maximo_responde_413()
    {
        var respuesta = await SubirAsync(Ana, UsuariosSemilla.Taller1, "grande.pdf", Pdf((int)EntregasApiFactory.MaxBytesPrueba + 1));

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, respuesta.StatusCode);
        Assert.Equal("ARCHIVO_DEMASIADO_GRANDE", (await ErrorDeAsync(respuesta)).Codigo);
        await respuesta.CumpleContratoAsync(Contrato, "post", Ruta);
    }

    [Fact]
    public async Task Archivo_del_tamano_maximo_exacto_se_acepta()
    {
        var respuesta = await SubirAsync(Ana, UsuariosSemilla.Taller1, "justo.pdf", Pdf((int)EntregasApiFactory.MaxBytesPrueba));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
    }

    [Fact]
    public async Task Exe_renombrado_a_pdf_responde_415()
    {
        byte[] exe = [0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00, 0x04, 0x00];

        var respuesta = await SubirAsync(Ana, UsuariosSemilla.Taller1, "tarea.pdf", exe);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, respuesta.StatusCode);
        Assert.Equal("TIPO_ARCHIVO_NO_PERMITIDO", (await ErrorDeAsync(respuesta)).Codigo);
        await respuesta.CumpleContratoAsync(Contrato, "post", Ruta);
        Assert.Empty(Api.Almacen.Blobs);
    }

    [Fact]
    public async Task Extension_no_permitida_responde_415()
    {
        var respuesta = await SubirAsync(Ana, UsuariosSemilla.Taller1, "virus.exe", Pdf());

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, respuesta.StatusCode);
        Assert.Equal("TIPO_ARCHIVO_NO_PERMITIDO", (await ErrorDeAsync(respuesta)).Codigo);
    }

    [Fact]
    public async Task Subir_dos_veces_reemplaza_el_archivo_y_deja_una_sola_fila()
    {
        var primera = await SubirOkAsync(Ana, UsuariosSemilla.Taller1, "v1.pdf");
        var rutaV1 = Assert.Single(Api.Almacen.Blobs).Key;
        Api.Reloj.Ahora = Api.Reloj.Ahora.AddMinutes(5);

        var segunda = await SubirOkAsync(Ana, UsuariosSemilla.Taller1, "v2.pdf", Pdf(4096));

        Assert.Equal(primera.Id, segunda.Id);
        var fila = Assert.Single(await FilasAsync());
        Assert.Equal(("v2.pdf", 4096L), (fila.NombreArchivo, fila.Tamano));
        Assert.Equal(Api.Reloj.Ahora.UtcDateTime, fila.FechaEnvio);
        var (rutaV2, _) = Assert.Single(Api.Almacen.Blobs);
        Assert.NotEqual(rutaV1, rutaV2);
    }

    [Fact]
    public async Task Estudiante_no_inscrito_recibe_403_SIN_PERMISO()
    {
        var respuesta = await SubirAsync(ClienteDe(UsuariosSemilla.Pedro), UsuariosSemilla.Taller1, "t.pdf", Pdf());

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Equal("SIN_PERMISO", (await ErrorDeAsync(respuesta)).Codigo);
        await respuesta.CumpleContratoAsync(Contrato, "post", Ruta);
    }

    [Fact]
    public async Task Actividad_inexistente_responde_404()
    {
        var respuesta = await SubirAsync(Ana, Guid.NewGuid(), "t.pdf", Pdf());

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Equal("NO_ENCONTRADO", (await ErrorDeAsync(respuesta)).Codigo);
        await respuesta.CumpleContratoAsync(Contrato, "post", Ruta);
    }

    [Fact]
    public async Task Sin_campo_archivo_responde_400_VALIDACION_FALLIDA()
    {
        var respuesta = await Ana.PostAsync($"/actividades/{UsuariosSemilla.Taller1}/entregas", new MultipartFormDataContent { { new StringContent("x"), "otro" } });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("VALIDACION_FALLIDA", (await ErrorDeAsync(respuesta)).Codigo);
        await respuesta.CumpleContratoAsync(Contrato, "post", Ruta);
    }

    [Fact]
    public async Task Actividad_con_id_invalido_responde_400()
    {
        var respuesta = await Ana.PostAsync("/actividades/no-es-guid/entregas", Archivo("t.pdf", Pdf()));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("VALIDACION_FALLIDA", (await ErrorDeAsync(respuesta)).Codigo);
    }

    [Fact]
    public async Task Evaluaciones_no_disponible_responde_503_sin_dejar_rastro()
    {
        Api.Evaluaciones.Caido = true;

        var respuesta = await SubirAsync(Ana, UsuariosSemilla.Taller1, "t.pdf", Pdf());

        Assert.Equal(HttpStatusCode.ServiceUnavailable, respuesta.StatusCode);
        Assert.Equal("SERVICIO_NO_DISPONIBLE", (await ErrorDeAsync(respuesta)).Codigo);
        Assert.Empty(await FilasAsync());
        Assert.Empty(Api.Almacen.Blobs);
    }
}
