using Evaluaciones.Domain.Entidades;

namespace Evaluaciones.Application.Abstracciones;

// Interfaces pequeñas y específicas (ISP). Las implementa Infrastructure con EF Core (DIP).

public interface ICursoRepository
{
    Task<Curso?> ObtenerAsync(Guid cursoId, CancellationToken ct);
    Task<IReadOnlyList<Curso>> ListarPorProfesorAsync(Guid profesorId, CancellationToken ct);
    Task<IReadOnlyList<Curso>> ListarPorEstudianteAsync(Guid estudianteId, CancellationToken ct);
}

public interface IInscripcionRepository
{
    Task<IReadOnlyList<CursoEstudiante>> ListarInscritosAsync(Guid cursoId, CancellationToken ct);
    Task<bool> EstaInscritoAsync(Guid cursoId, Guid estudianteId, CancellationToken ct);
}

public interface IActividadRepository
{
    Task<Actividad?> ObtenerActividadAsync(Guid actividadId, CancellationToken ct);
    Task<IReadOnlyList<Actividad>> ListarActividadesAsync(Guid cursoId, CancellationToken ct);
    Task<IReadOnlyList<Actividad>> ListarActividadesDeCursosAsync(IReadOnlyCollection<Guid> cursoIds, CancellationToken ct);
}

public interface ICalificacionRepository
{
    /// <summary>Calificaciones de todos los estudiantes en las actividades del curso, en cualquier estado.</summary>
    Task<IReadOnlyList<Calificacion>> ListarCalificacionesDelCursoAsync(Guid cursoId, CancellationToken ct);

    /// <summary>Calificaciones en estado PUBLICADA del estudiante, en cualquier curso (lo único que él puede ver).</summary>
    Task<IReadOnlyList<Calificacion>> ListarCalificacionesPublicadasAsync(Guid estudianteId, CancellationToken ct);
}

public interface IPublicacionCorteRepository
{
    Task<IReadOnlyList<PublicacionCorte>> ListarPublicacionesDelCursoAsync(Guid cursoId, CancellationToken ct);

    /// <summary>Cortes publicados del estudiante en todos sus cursos (solo lectura, CU-16).</summary>
    Task<IReadOnlyList<PublicacionCorte>> ListarPublicacionesDelEstudianteAsync(Guid estudianteId, CancellationToken ct);
    Task<PublicacionCorte?> ObtenerPublicacionAsync(Guid cursoId, Guid estudianteId, int corte, CancellationToken ct);
    void AgregarPublicacion(PublicacionCorte publicacion);

    /// <summary>Exige que la versión guardada coincida con la que leyó el cliente (If-Match, sección 9.6).</summary>
    void ExigirVersion(PublicacionCorte publicacion, byte[] versionEsperada);
}

public interface IUnidadDeTrabajo
{
    /// <exception cref="Evaluaciones.Domain.Errores.DominioException">CONFLICTO_CONCURRENCIA si otro cambio modificó el registro.</exception>
    Task GuardarCambiosAsync(CancellationToken ct);
}
