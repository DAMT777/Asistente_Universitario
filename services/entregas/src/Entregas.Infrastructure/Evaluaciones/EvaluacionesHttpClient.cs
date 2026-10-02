using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Entregas.Application.Puertos;

namespace Entregas.Infrastructure.Evaluaciones;

/// <summary>
/// GET /internal/actividades/{id}?estudianteId= en Evaluaciones. Timeout configurado en el HttpClient;
/// sin reintentos. Cualquier falla de red, timeout o 5xx se traduce a SERVICIO_NO_DISPONIBLE.
/// </summary>
public sealed class EvaluacionesHttpClient(HttpClient http, ILogger<EvaluacionesHttpClient> logger) : IEvaluacionesClient
{
    private const string Servicio = "Evaluaciones";

    public async Task<ActividadInfo?> ObtenerActividadAsync(Guid actividadId, Guid? estudianteId, CancellationToken ct)
    {
        try
        {
            var consulta = estudianteId is { } id ? $"?estudianteId={id}" : "";
            using var respuesta = await http.GetAsync($"internal/actividades/{actividadId}{consulta}", ct);
            if (respuesta.StatusCode == HttpStatusCode.NotFound) return null;
            if (!respuesta.IsSuccessStatusCode)
            {
                logger.LogWarning("Evaluaciones respondió {Estado} al consultar la actividad {ActividadId}", (int)respuesta.StatusCode, actividadId);
                throw new ServicioNoDisponibleException(Servicio);
            }

            var dto = await respuesta.Content.ReadFromJsonAsync<ActividadInternaDto>(ct)
                      ?? throw new ServicioNoDisponibleException(Servicio);
            return new ActividadInfo(dto.ActividadId, dto.CursoId, dto.ProfesorId, dto.FechaLimite, dto.RequiereEntrega, dto.EstudianteInscrito);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning("Timeout consultando Evaluaciones para la actividad {ActividadId}", actividadId);
            throw new ServicioNoDisponibleException(Servicio, ex);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Evaluaciones no disponible al consultar la actividad {ActividadId}", actividadId);
            throw new ServicioNoDisponibleException(Servicio, ex);
        }
        catch (System.Text.Json.JsonException ex)
        {
            logger.LogWarning(ex, "Respuesta inválida de Evaluaciones para la actividad {ActividadId}", actividadId);
            throw new ServicioNoDisponibleException(Servicio, ex);
        }
    }

    private sealed record ActividadInternaDto(
        Guid ActividadId,
        Guid CursoId,
        Guid ProfesorId,
        DateTimeOffset? FechaLimite,
        bool RequiereEntrega,
        bool EstudianteInscrito);
}
