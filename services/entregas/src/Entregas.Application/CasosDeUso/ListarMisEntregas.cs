using Entregas.Application.Comun;
using Entregas.Application.Puertos;

namespace Entregas.Application.CasosDeUso;

/// <summary>Entregas propias del estudiante, opcionalmente filtradas por actividad.</summary>
public sealed class ListarMisEntregas(IEntregaRepositorio repositorio)
{
    public async Task<IReadOnlyList<EntregaRespuesta>> EjecutarAsync(Guid estudianteId, Guid? actividadId, CancellationToken ct)
    {
        var entregas = await repositorio.ListarDelEstudianteAsync(estudianteId, actividadId, ct);
        return entregas.Select(EntregaRespuesta.Desde).ToList();
    }
}
