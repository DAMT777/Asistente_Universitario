using Unillanos.Entregas.Application.Puertos;
using Unillanos.Entregas.Domain;

namespace Unillanos.Entregas.Application.CasosDeUso;

/// <summary>RN-16: una entrega ajena se trata igual que una inexistente (404, no revela existencia).</summary>
internal static class BuscarPropia
{
    public static async Task<Entrega> EjecutarAsync(IEntregaRepositorio repositorio, Guid entregaId, Guid estudianteId, CancellationToken ct)
    {
        var entrega = await repositorio.ObtenerAsync(entregaId, ct);
        return entrega is not null && entrega.PerteneceA(estudianteId)
            ? entrega
            : throw new NoEncontradoException("La entrega");
    }
}
