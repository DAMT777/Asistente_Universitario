using Evaluaciones.Application.Abstracciones;
using Evaluaciones.Domain.Entidades;

namespace Evaluaciones.UnitTests.Apoyo;

/// <summary>Implementación en memoria de todos los repositorios, para probar los casos de uso sin base de datos (LSP).</summary>
public sealed class BaseEnMemoria :
    ICursoRepository,
    IInscripcionRepository,
    IActividadRepository,
    ICalificacionRepository,
    IPublicacionCorteRepository,
    IUnidadDeTrabajo
{
    public List<Curso> Cursos { get; } = [];
    public List<CursoEstudiante> Inscripciones { get; } = [];
    public List<Actividad> Actividades { get; } = [];
    public List<Calificacion> Calificaciones { get; } = [];
    public List<PublicacionCorte> Publicaciones { get; } = [];

    public byte[]? VersionExigida { get; private set; }
    public int GuardadosRealizados { get; private set; }

    public Task<Curso?> ObtenerAsync(Guid cursoId, CancellationToken ct) =>
        Task.FromResult(Cursos.FirstOrDefault(c => c.Id == cursoId));

    public Task<IReadOnlyList<Curso>> ListarPorProfesorAsync(Guid profesorId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Curso>>(Cursos.Where(c => c.ProfesorId == profesorId).ToList());

    public Task<IReadOnlyList<Curso>> ListarPorEstudianteAsync(Guid estudianteId, CancellationToken ct)
    {
        var ids = Inscripciones.Where(i => i.EstudianteId == estudianteId).Select(i => i.CursoId).ToHashSet();
        return Task.FromResult<IReadOnlyList<Curso>>(Cursos.Where(c => ids.Contains(c.Id)).ToList());
    }

    public Task<IReadOnlyList<CursoEstudiante>> ListarInscritosAsync(Guid cursoId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<CursoEstudiante>>(Inscripciones.Where(i => i.CursoId == cursoId).ToList());

    public Task<bool> EstaInscritoAsync(Guid cursoId, Guid estudianteId, CancellationToken ct) =>
        Task.FromResult(Inscripciones.Any(i => i.CursoId == cursoId && i.EstudianteId == estudianteId));

    public Task<Actividad?> ObtenerActividadAsync(Guid actividadId, CancellationToken ct) =>
        Task.FromResult(Actividades.FirstOrDefault(a => a.Id == actividadId));

    public Task<IReadOnlyList<Actividad>> ListarActividadesAsync(Guid cursoId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Actividad>>(Actividades.Where(a => a.CursoId == cursoId).ToList());

    public Task<IReadOnlyList<Actividad>> ListarActividadesDeCursosAsync(
        IReadOnlyCollection<Guid> cursoIds, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Actividad>>(Actividades.Where(a => cursoIds.Contains(a.CursoId)).ToList());

    public Task<IReadOnlyList<Calificacion>> ListarCalificacionesDelCursoAsync(Guid cursoId, CancellationToken ct)
    {
        var ids = Actividades.Where(a => a.CursoId == cursoId).Select(a => a.Id).ToHashSet();
        return Task.FromResult<IReadOnlyList<Calificacion>>(
            Calificaciones.Where(c => ids.Contains(c.ActividadId)).ToList());
    }

    public Task<IReadOnlyList<Calificacion>> ListarCalificacionesPublicadasAsync(Guid estudianteId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Calificacion>>(
            Calificaciones.Where(c => c.EstudianteId == estudianteId && c.Estado == EstadoCalificacion.Publicada).ToList());

    public Task<IReadOnlyList<PublicacionCorte>> ListarPublicacionesDelCursoAsync(Guid cursoId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<PublicacionCorte>>(Publicaciones.Where(p => p.CursoId == cursoId).ToList());

    public Task<PublicacionCorte?> ObtenerPublicacionAsync(Guid cursoId, Guid estudianteId, int corte, CancellationToken ct) =>
        Task.FromResult(Publicaciones.FirstOrDefault(
            p => p.CursoId == cursoId && p.EstudianteId == estudianteId && p.Corte == corte));

    public void AgregarPublicacion(PublicacionCorte publicacion) => Publicaciones.Add(publicacion);

    public void ExigirVersion(PublicacionCorte publicacion, byte[] versionEsperada) => VersionExigida = versionEsperada;

    public Task GuardarCambiosAsync(CancellationToken ct)
    {
        GuardadosRealizados++;
        return Task.CompletedTask;
    }
}
