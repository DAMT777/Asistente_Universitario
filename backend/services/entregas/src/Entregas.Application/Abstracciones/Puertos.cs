using Entregas.Domain;

namespace Entregas.Application.Abstracciones;

// Interfaces pequeñas (ISP). Las implementa Infrastructure (DIP).

public interface IEntregaRepository
{
    Task<IReadOnlyList<Entrega>> ListarPorActividadAsync(Guid actividadId, CancellationToken ct);
    Task<IReadOnlyList<Entrega>> ListarPorEstudianteAsync(Guid estudianteId, Guid? actividadId, CancellationToken ct);
    Task<Entrega?> ObtenerAsync(Guid entregaId, CancellationToken ct);
}

/// <summary>Almacenamiento de los archivos (Blob Storage en Azure, disco local en desarrollo). Contenedor privado.</summary>
public interface IAlmacenArchivos
{
    /// <returns>Nulo si el archivo no existe.</returns>
    Task<Stream?> AbrirAsync(string ruta, CancellationToken ct);
}

/// <summary>Datos que entrega Evaluaciones en GET /internal/actividades/{id} (sección 8.6).</summary>
public sealed record ActividadInfo(
    Guid ActividadId,
    Guid CursoId,
    Guid ProfesorId,
    DateTime? FechaLimite,
    bool RequiereEntrega,
    bool? EstudianteInscrito);

/// <summary>
/// La única llamada entre servicios: Entregas consulta a Evaluaciones. Si Evaluaciones no responde, lanza
/// SERVICIO_NO_DISPONIBLE y nunca asume datos (sección 6).
/// </summary>
public interface IEvaluacionesCliente
{
    /// <returns>Nulo si la actividad no existe.</returns>
    Task<ActividadInfo?> ObtenerActividadAsync(Guid actividadId, Guid? estudianteId, CancellationToken ct);
}
