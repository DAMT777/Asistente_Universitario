using Evaluaciones.Application.Abstracciones;
using Evaluaciones.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Evaluaciones.Infrastructure.Repositorios;

internal sealed class ActividadRepository(EvaluacionesDbContext db) : IActividadRepository
{
    public async Task<ContextoActividad?> ObtenerContextoAsync(Guid actividadId, CancellationToken ct)
    {
        var fila = await (
            from a in db.Actividades.AsNoTracking()
            join c in db.Cursos.AsNoTracking() on a.CursoId equals c.Id
            where a.Id == actividadId
            select new { Actividad = a, c.ProfesorId })
            .FirstOrDefaultAsync(ct);

        return fila is null ? null : new ContextoActividad(fila.Actividad, fila.ProfesorId);
    }

    public Task<bool> EstudianteInscritoAsync(Guid cursoId, Guid estudianteId, CancellationToken ct) =>
        db.CursoEstudiantes.AnyAsync(x => x.CursoId == cursoId && x.EstudianteId == estudianteId, ct);
}
