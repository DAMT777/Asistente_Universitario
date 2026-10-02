using System.Net;
using System.Net.Http.Json;
using Entregas.Application.Abstracciones;
using Entregas.Domain;

namespace Entregas.Infrastructure.Clientes;

/// <summary>
/// GET /internal/actividades/{id} en Evaluaciones con X-Service-Key (secciones 6, 8.6 y 12.4).
/// Cualquier falla de red, tiempo agotado o error 5xx se convierte en SERVICIO_NO_DISPONIBLE: nunca se asume
/// que la actividad es válida.
/// </summary>
public sealed class EvaluacionesClienteHttp(HttpClient http) : IEvaluacionesCliente
{
    public const string EncabezadoClave = "X-Service-Key";

    public async Task<ActividadInfo?> ObtenerActividadAsync(Guid actividadId, Guid? estudianteId, CancellationToken ct)
    {
        var ruta = $"internal/actividades/{actividadId}" + (estudianteId is null ? "" : $"?estudianteId={estudianteId}");

        HttpResponseMessage respuesta;
        try
        {
            respuesta = await http.GetAsync(ruta, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            throw NoDisponible();
        }

        using (respuesta)
        {
            if (respuesta.StatusCode == HttpStatusCode.NotFound) return null;
            if (!respuesta.IsSuccessStatusCode) throw NoDisponible();

            try
            {
                return await respuesta.Content.ReadFromJsonAsync<ActividadInfo>(ct) ?? throw NoDisponible();
            }
            catch (System.Text.Json.JsonException)
            {
                throw NoDisponible();
            }
        }
    }

    private static DominioException NoDisponible() => new(
        CodigosError.ServicioNoDisponible,
        "El servicio de evaluaciones no respondió. Intenta de nuevo en unos segundos.");
}
