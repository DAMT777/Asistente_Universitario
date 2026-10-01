using Unillanos.Evaluaciones.Domain.Entidades;

namespace Unillanos.Evaluaciones.Application.Puertos;

public interface ICursoRepositorio
{
    Task<Curso?> ObtenerAsync(Guid cursoId, CancellationToken ct);
    Task<bool> EstaInscritoAsync(Guid cursoId, Guid estudianteId, CancellationToken ct);
    Task<IReadOnlyList<Curso>> ListarDeEstudianteAsync(Guid estudianteId, CancellationToken ct);
}

public interface IActividadRepositorio
{
    Task<Actividad?> ObtenerAsync(Guid actividadId, CancellationToken ct);
    Task<IReadOnlyList<Actividad>> ListarDeCursoAsync(Guid cursoId, CancellationToken ct);
}

public interface ICalificacionRepositorio
{
    /// <summary>Solo calificaciones PUBLICADAS del estudiante: los borradores nunca salen de la base.</summary>
    Task<IReadOnlyList<Calificacion>> ListarPublicadasAsync(Guid estudianteId, IReadOnlyCollection<Guid> actividadIds, CancellationToken ct);
}

public interface IPublicacionCorteRepositorio
{
    /// <summary>Última versión publicada de cada corte del estudiante en los cursos indicados.</summary>
    Task<IReadOnlyList<PublicacionCorte>> ListarVigentesAsync(Guid estudianteId, IReadOnlyCollection<Guid> cursoIds, CancellationToken ct);
}
