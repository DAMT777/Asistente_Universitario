namespace Entregas.Application.Puertos;

/// <summary>
/// Única llamada entre servicios: GET /internal/actividades/{id}?estudianteId= en Evaluaciones.
/// Devuelve null si la actividad no existe. Lanza <see cref="ServicioNoDisponibleException"/>
/// si Evaluaciones no responde a tiempo o falla.
/// </summary>
public interface IEvaluacionesClient
{
    /// <param name="estudianteId">Null cuando consulta un profesor: EstudianteInscrito llega en false.</param>
    Task<ActividadInfo?> ObtenerActividadAsync(Guid actividadId, Guid? estudianteId, CancellationToken ct);
}

public sealed record ActividadInfo(
    Guid ActividadId,
    Guid CursoId,
    Guid ProfesorId,
    /// <summary>Null si la actividad no tiene fecha límite.</summary>
    DateTimeOffset? FechaLimite,
    bool RequiereEntrega,
    bool EstudianteInscrito);

public sealed class ServicioNoDisponibleException(string servicio, Exception? causa = null)
    : Exception($"El servicio {servicio} no está disponible.", causa)
{
    public const string Codigo = "SERVICIO_NO_DISPONIBLE";
}
