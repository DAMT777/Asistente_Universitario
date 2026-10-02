using Evaluaciones.Domain.Calificaciones;

namespace Evaluaciones.Application.Abstracciones;

public interface ICalificacionRepository
{
    Task<Calificacion?> ObtenerAsync(Guid actividadId, Guid estudianteId, CancellationToken ct);
    Task<IReadOnlyList<Calificacion>> ListarPorActividadAsync(Guid actividadId, CancellationToken ct);
    /// <summary>Calificaciones en borrador de la actividad; si se pasan estudiantes, solo las de ellos.</summary>
    Task<IReadOnlyList<Calificacion>> ListarBorradoresAsync(Guid actividadId, IReadOnlyCollection<Guid>? estudianteIds, CancellationToken ct);
    void Agregar(Calificacion calificacion);
}
