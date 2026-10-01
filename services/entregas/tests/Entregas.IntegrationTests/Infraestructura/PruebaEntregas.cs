using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Entregas.Domain;
using Entregas.Infrastructure.Persistencia;
using Unillanos.Pruebas.Compartidas;
using Unillanos.Pruebas.Compartidas.Contratos;

namespace Entregas.IntegrationTests.Infraestructura;

/// <summary>Base de las pruebas: limpia la base, el almacén y el doble de Evaluaciones antes de cada prueba.</summary>
[Collection(ColeccionEntregas.Nombre)]
public abstract class PruebaEntregas(EntregasFixture fixture) : IAsyncLifetime
{
    protected static readonly ContratoOpenApi Contrato = ContratoOpenApi.Cargar("entregas.yaml");
    protected EntregasFixture Fixture { get; } = fixture;
    protected virtual EntregasApiFactory Api => Fixture.Api;

    public virtual async ValueTask InitializeAsync()
    {
        Api.Reloj.Ahora = RelojFijo.AhoraTruncado();
        Api.Evaluaciones.Reiniciar();
        Api.Almacen.Blobs.Clear();
        await using var scope = Api.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<EntregasDbContext>().Entregas.ExecuteDeleteAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    protected HttpClient ClienteDe(Guid usuarioId, string rol = "ESTUDIANTE", string nombre = "Estudiante", bool expirado = false)
    {
        var cliente = Api.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Fixture.Llaves.CrearToken(usuarioId, rol, nombre, expirado));
        return cliente;
    }

    protected HttpClient Ana => ClienteDe(UsuariosSemilla.Ana, nombre: "Ana");
    protected HttpClient Luis => ClienteDe(UsuariosSemilla.Luis, nombre: "Luis");

    protected static MultipartFormDataContent Archivo(string nombre, byte[] datos)
    {
        var parte = new ByteArrayContent(datos);
        parte.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        return new MultipartFormDataContent { { parte, "archivo", nombre } };
    }

    protected static byte[] Pdf(int tamano = 2048, byte relleno = 0x20)
    {
        var datos = Enumerable.Repeat(relleno, tamano).ToArray();
        "%PDF-1.7\n"u8.CopyTo(datos);
        return datos;
    }

    protected static Task<HttpResponseMessage> SubirAsync(HttpClient cliente, Guid actividadId, string nombre, byte[] datos)
        => cliente.PostAsync($"/actividades/{actividadId}/entregas", Archivo(nombre, datos));

    protected async Task<EntregaDto> SubirOkAsync(HttpClient cliente, Guid actividadId, string nombre = "taller1.pdf", byte[]? datos = null)
    {
        var respuesta = await SubirAsync(cliente, actividadId, nombre, datos ?? Pdf());
        Assert.Equal(System.Net.HttpStatusCode.Created, respuesta.StatusCode);
        return (await respuesta.Content.ReadFromJsonAsync<EntregaDto>())!;
    }

    protected async Task<List<Entrega>> FilasAsync()
    {
        await using var scope = Api.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<EntregasDbContext>().Entregas.AsNoTracking().ToListAsync();
    }

    protected static async Task<ErrorDto> ErrorDeAsync(HttpResponseMessage respuesta)
        => (await respuesta.Content.ReadFromJsonAsync<ErrorDto>())!;

    public sealed record EntregaDto(Guid Id, Guid ActividadId, Guid EstudianteId, DateTimeOffset FechaEnvio, string Estado, string NombreArchivo, long Tamano);

    public sealed record ErrorDto(int Status, string Codigo, string Mensaje, string TraceId);
}
