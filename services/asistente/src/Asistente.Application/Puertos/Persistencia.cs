using Asistente.Domain;

namespace Asistente.Application.Puertos;

public interface IAccionesRepositorio
{
    Task AgregarAsync(AccionPendiente accion, CancellationToken ct);
    /// <summary>Solo la encuentra si la creó ese usuario; para cualquier otro no existe.</summary>
    Task<AccionPendiente?> BuscarDeUsuarioAsync(Guid id, Guid usuarioId, CancellationToken ct);
    /// <summary>Pasa de PENDIENTE a <paramref name="nuevo"/> de forma atómica. False si otra solicitud ya la tomó.</summary>
    Task<bool> TomarAsync(Guid id, EstadoAccion nuevo, DateTimeOffset ahora, CancellationToken ct);
    Task ResolverAsync(Guid id, EstadoAccion estado, DateTimeOffset ahora, CancellationToken ct);
}

public interface IAuditoria
{
    Task RegistrarAsync(RegistroAuditoria registro, CancellationToken ct);
}
