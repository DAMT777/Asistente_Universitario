using Unillanos.Entregas.Domain;

namespace Unillanos.Entregas.Application.Puertos;

public interface IEntregaRepositorio
{
    Task<Entrega?> ObtenerAsync(Guid entregaId, CancellationToken ct);
    Task<Entrega?> ObtenerPorActividadYEstudianteAsync(Guid actividadId, Guid estudianteId, CancellationToken ct);
    Task<IReadOnlyList<Entrega>> ListarDelEstudianteAsync(Guid estudianteId, Guid? actividadId, CancellationToken ct);
    void Agregar(Entrega entrega);
    Task GuardarCambiosAsync(CancellationToken ct);
}
