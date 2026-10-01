using Microsoft.EntityFrameworkCore;
using Unillanos.Entregas.Application.Puertos;
using Unillanos.Entregas.Domain;

namespace Unillanos.Entregas.Infrastructure.Persistencia;

internal sealed class EntregaRepositorio(EntregasDbContext db) : IEntregaRepositorio
{
    public Task<Entrega?> ObtenerAsync(Guid entregaId, CancellationToken ct)
        => db.Entregas.FirstOrDefaultAsync(e => e.Id == entregaId, ct);

    public Task<Entrega?> ObtenerPorActividadYEstudianteAsync(Guid actividadId, Guid estudianteId, CancellationToken ct)
        => db.Entregas.FirstOrDefaultAsync(e => e.ActividadId == actividadId && e.EstudianteId == estudianteId, ct);

    public async Task<IReadOnlyList<Entrega>> ListarDelEstudianteAsync(Guid estudianteId, Guid? actividadId, CancellationToken ct)
    {
        var consulta = db.Entregas.AsNoTracking().Where(e => e.EstudianteId == estudianteId);
        if (actividadId is { } id) consulta = consulta.Where(e => e.ActividadId == id);
        return await consulta.OrderByDescending(e => e.FechaEnvio).ToListAsync(ct);
    }

    public void Agregar(Entrega entrega) => db.Entregas.Add(entrega);

    public Task GuardarCambiosAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
