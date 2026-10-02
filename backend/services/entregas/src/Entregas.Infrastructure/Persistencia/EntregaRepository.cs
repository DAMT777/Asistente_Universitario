using Entregas.Application.Abstracciones;
using Entregas.Domain;
using Microsoft.EntityFrameworkCore;

namespace Entregas.Infrastructure.Persistencia;

internal sealed class EntregaRepository(EntregasDbContext db) : IEntregaRepository
{
    public async Task<IReadOnlyList<Entrega>> ListarPorActividadAsync(Guid actividadId, CancellationToken ct) =>
        await db.Entregas.AsNoTracking().Where(e => e.ActividadId == actividadId).ToListAsync(ct);

    public async Task<IReadOnlyList<Entrega>> ListarPorEstudianteAsync(Guid estudianteId, Guid? actividadId, CancellationToken ct) =>
        await db.Entregas.AsNoTracking()
            .Where(e => e.EstudianteId == estudianteId && (actividadId == null || e.ActividadId == actividadId))
            .ToListAsync(ct);

    public Task<Entrega?> ObtenerAsync(Guid entregaId, CancellationToken ct) =>
        db.Entregas.AsNoTracking().FirstOrDefaultAsync(e => e.Id == entregaId, ct);
}
