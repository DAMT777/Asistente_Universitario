using Evaluaciones.Application.Abstracciones;
using Evaluaciones.Domain.Entidades;
using Evaluaciones.Domain.Errores;
using Microsoft.EntityFrameworkCore;

namespace Evaluaciones.Infrastructure.Persistencia;

internal sealed class CursoRepository(EvaluacionesDbContext db) : ICursoRepository
{
    public Task<Curso?> ObtenerAsync(Guid cursoId, CancellationToken ct) =>
        db.Cursos.AsNoTracking().FirstOrDefaultAsync(c => c.Id == cursoId, ct);

    public async Task<IReadOnlyList<Curso>> ListarPorProfesorAsync(Guid profesorId, CancellationToken ct) =>
        await db.Cursos.AsNoTracking()
            .Where(c => c.ProfesorId == profesorId)
            .OrderBy(c => c.Codigo)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Curso>> ListarPorEstudianteAsync(Guid estudianteId, CancellationToken ct) =>
        await db.Inscripciones.AsNoTracking()
            .Where(i => i.EstudianteId == estudianteId)
            .Join(db.Cursos.AsNoTracking(), i => i.CursoId, c => c.Id, (_, c) => c)
            .OrderBy(c => c.Codigo)
            .ToListAsync(ct);
}

internal sealed class InscripcionRepository(EvaluacionesDbContext db) : IInscripcionRepository
{
    public async Task<IReadOnlyList<CursoEstudiante>> ListarInscritosAsync(Guid cursoId, CancellationToken ct) =>
        await db.Inscripciones.AsNoTracking().Where(i => i.CursoId == cursoId).ToListAsync(ct);

    public Task<bool> EstaInscritoAsync(Guid cursoId, Guid estudianteId, CancellationToken ct) =>
        db.Inscripciones.AsNoTracking().AnyAsync(i => i.CursoId == cursoId && i.EstudianteId == estudianteId, ct);
}

internal sealed class ActividadRepository(EvaluacionesDbContext db) : IActividadRepository
{
    public Task<Actividad?> ObtenerActividadAsync(Guid actividadId, CancellationToken ct) =>
        db.Actividades.AsNoTracking().FirstOrDefaultAsync(a => a.Id == actividadId, ct);

    public async Task<IReadOnlyList<Actividad>> ListarActividadesAsync(Guid cursoId, CancellationToken ct) =>
        await db.Actividades.AsNoTracking().Where(a => a.CursoId == cursoId).ToListAsync(ct);

    public async Task<IReadOnlyList<Actividad>> ListarActividadesDeCursosAsync(
        IReadOnlyCollection<Guid> cursoIds, CancellationToken ct) =>
        await db.Actividades.AsNoTracking().Where(a => cursoIds.Contains(a.CursoId)).ToListAsync(ct);
}

internal sealed class CalificacionRepository(EvaluacionesDbContext db) : ICalificacionRepository
{
    public async Task<IReadOnlyList<Calificacion>> ListarCalificacionesDelCursoAsync(Guid cursoId, CancellationToken ct) =>
        await db.Calificaciones.AsNoTracking()
            .Join(db.Actividades.Where(a => a.CursoId == cursoId), c => c.ActividadId, a => a.Id, (c, _) => c)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Calificacion>> ListarCalificacionesPublicadasAsync(Guid estudianteId, CancellationToken ct) =>
        await db.Calificaciones.AsNoTracking()
            .Where(c => c.EstudianteId == estudianteId && c.Estado == EstadoCalificacion.Publicada)
            .ToListAsync(ct);
}

internal sealed class PublicacionCorteRepository(EvaluacionesDbContext db) : IPublicacionCorteRepository
{
    // Con seguimiento: los casos de uso modifican estas entidades y la unidad de trabajo las guarda.
    public async Task<IReadOnlyList<PublicacionCorte>> ListarPublicacionesDelCursoAsync(Guid cursoId, CancellationToken ct) =>
        await db.PublicacionesCorte.Where(p => p.CursoId == cursoId).ToListAsync(ct);

    public async Task<IReadOnlyList<PublicacionCorte>> ListarPublicacionesDelEstudianteAsync(Guid estudianteId, CancellationToken ct) =>
        await db.PublicacionesCorte.AsNoTracking().Where(p => p.EstudianteId == estudianteId).ToListAsync(ct);

    public Task<PublicacionCorte?> ObtenerPublicacionAsync(Guid cursoId, Guid estudianteId, int corte, CancellationToken ct) =>
        db.PublicacionesCorte.FirstOrDefaultAsync(
            p => p.CursoId == cursoId && p.EstudianteId == estudianteId && p.Corte == corte, ct);

    public void AgregarPublicacion(PublicacionCorte publicacion) => db.PublicacionesCorte.Add(publicacion);

    public void ExigirVersion(PublicacionCorte publicacion, byte[] versionEsperada) =>
        db.Entry(publicacion).Property(p => p.Version).OriginalValue = versionEsperada;
}

internal sealed class UnidadDeTrabajo(EvaluacionesDbContext db) : IUnidadDeTrabajo
{
    public async Task GuardarCambiosAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new DominioException(
                CodigosError.ConflictoConcurrencia,
                "Otro cambio modificó el mismo registro. Consulta de nuevo e inténtalo otra vez.");
        }
    }
}
