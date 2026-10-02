using Entregas.Domain;

namespace Entregas.Application.Puertos;

public interface IEntregaRepositorio
{
    Task<Entrega?> ObtenerAsync(Guid entregaId, CancellationToken ct);
    Task<Entrega?> ObtenerPorActividadYEstudianteAsync(Guid actividadId, Guid estudianteId, CancellationToken ct);
    Task<IReadOnlyList<Entrega>> ListarDelEstudianteAsync(Guid estudianteId, Guid? actividadId, CancellationToken ct);
    Task<IReadOnlyList<Entrega>> ListarDeActividadAsync(Guid actividadId, CancellationToken ct);
    void Agregar(Entrega entrega);
    Task GuardarCambiosAsync(CancellationToken ct);
}
