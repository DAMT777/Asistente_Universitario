using Asistente.Application.Puertos;
using Asistente.Domain;
using Microsoft.EntityFrameworkCore;

namespace Asistente.Infrastructure.Persistencia;

public sealed class AccionesRepositorio(AsistenteDbContext db) : IAccionesRepositorio
{
    public async Task AgregarAsync(AccionPendiente accion, CancellationToken ct)
    {
        db.Acciones.Add(accion);
        await db.SaveChangesAsync(ct);
    }

    public Task<AccionPendiente?> BuscarDeUsuarioAsync(Guid id, Guid usuarioId, CancellationToken ct) =>
        db.Acciones.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id && a.CreadaPor == usuarioId, ct);

    public async Task<bool> TomarAsync(Guid id, EstadoAccion nuevo, DateTimeOffset ahora, CancellationToken ct) =>
        await db.Acciones
            .Where(a => a.Id == id && a.Estado == EstadoAccion.Pendiente && a.ExpiraEn > ahora)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.Estado, nuevo).SetProperty(a => a.ResueltaEn, ahora), ct) == 1;

    public Task ResolverAsync(Guid id, EstadoAccion estado, DateTimeOffset ahora, CancellationToken ct) =>
        db.Acciones.Where(a => a.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.Estado, estado).SetProperty(a => a.ResueltaEn, ahora), ct);
}

public sealed class AuditoriaRepositorio(AsistenteDbContext db) : IAuditoria
{
    public async Task RegistrarAsync(RegistroAuditoria registro, CancellationToken ct)
    {
        db.Auditoria.Add(registro);
        await db.SaveChangesAsync(ct);
    }
}
