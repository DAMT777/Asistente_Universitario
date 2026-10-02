using Evaluaciones.Application.Abstracciones;
using Evaluaciones.Domain.Calificaciones;
using Evaluaciones.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Evaluaciones.Infrastructure.Repositorios;

internal sealed class CalificacionRepository(EvaluacionesDbContext db) : ICalificacionRepository
{
    // Las consultas que luego se modifican van con seguimiento: EF compara la versión al guardar.
    public Task<Calificacion?> ObtenerAsync(Guid actividadId, Guid estudianteId, CancellationToken ct) =>
        db.Calificaciones.FirstOrDefaultAsync(c => c.ActividadId == actividadId && c.EstudianteId == estudianteId, ct);

    public async Task<IReadOnlyList<Calificacion>> ListarPorActividadAsync(Guid actividadId, CancellationToken ct) =>
        await db.Calificaciones.AsNoTracking()
            .Where(c => c.ActividadId == actividadId)
            .OrderBy(c => c.EstudianteId)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Calificacion>> ListarBorradoresAsync(Guid actividadId, IReadOnlyCollection<Guid>? estudianteIds, CancellationToken ct)
    {
        var consulta = db.Calificaciones.Where(c => c.ActividadId == actividadId && c.Estado == EstadoCalificacion.Borrador);
        if (estudianteIds is not null)
        {
            var ids = estudianteIds.ToArray();
            consulta = consulta.Where(c => ids.Contains(c.EstudianteId));
        }
        return await consulta.ToListAsync(ct);
    }

    public void Agregar(Calificacion calificacion) => db.Calificaciones.Add(calificacion);
}
