using Microsoft.EntityFrameworkCore;
using Unillanos.Evaluaciones.Application.Puertos;
using Unillanos.Evaluaciones.Domain.Entidades;

namespace Unillanos.Evaluaciones.Infrastructure.Persistencia;

internal sealed class CursoRepositorio(EvaluacionesDbContext db) : ICursoRepositorio
{
    public Task<Curso?> ObtenerAsync(Guid cursoId, CancellationToken ct)
        => db.Cursos.AsNoTracking().FirstOrDefaultAsync(c => c.Id == cursoId, ct);

    public Task<bool> EstaInscritoAsync(Guid cursoId, Guid estudianteId, CancellationToken ct)
        => db.CursoEstudiantes.AnyAsync(ce => ce.CursoId == cursoId && ce.EstudianteId == estudianteId, ct);

    public async Task<IReadOnlyList<Curso>> ListarDeEstudianteAsync(Guid estudianteId, CancellationToken ct)
        => await db.Cursos.AsNoTracking()
            .Where(c => db.CursoEstudiantes.Any(ce => ce.CursoId == c.Id && ce.EstudianteId == estudianteId))
            .ToListAsync(ct);
}

internal sealed class ActividadRepositorio(EvaluacionesDbContext db) : IActividadRepositorio
{
    public Task<Actividad?> ObtenerAsync(Guid actividadId, CancellationToken ct)
        => db.Actividades.AsNoTracking().FirstOrDefaultAsync(a => a.Id == actividadId, ct);

    public async Task<IReadOnlyList<Actividad>> ListarDeCursoAsync(Guid cursoId, CancellationToken ct)
        => await db.Actividades.AsNoTracking().Where(a => a.CursoId == cursoId).ToListAsync(ct);
}

internal sealed class CalificacionRepositorio(EvaluacionesDbContext db) : ICalificacionRepositorio
{
    public async Task<IReadOnlyList<Calificacion>> ListarPublicadasAsync(Guid estudianteId, IReadOnlyCollection<Guid> actividadIds, CancellationToken ct)
        => await db.Calificaciones.AsNoTracking()
            .Where(c => c.EstudianteId == estudianteId
                        && c.Estado == EstadoCalificacion.PUBLICADA
                        && actividadIds.Contains(c.ActividadId))
            .ToListAsync(ct);
}

internal sealed class PublicacionCorteRepositorio(EvaluacionesDbContext db) : IPublicacionCorteRepositorio
{
    public async Task<IReadOnlyList<PublicacionCorte>> ListarVigentesAsync(Guid estudianteId, IReadOnlyCollection<Guid> cursoIds, CancellationToken ct)
    {
        var todas = await db.PublicacionesCorte.AsNoTracking()
            .Where(p => p.EstudianteId == estudianteId && cursoIds.Contains(p.CursoId))
            .ToListAsync(ct);

        // Pocas filas por estudiante: la última versión de cada corte se elige en memoria.
        return todas
            .GroupBy(p => (p.CursoId, p.Corte))
            .Select(g => g.MaxBy(p => p.Version)!)
            .ToList();
    }
}
