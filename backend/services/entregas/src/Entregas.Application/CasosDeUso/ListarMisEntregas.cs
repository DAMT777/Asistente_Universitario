using Entregas.Application.Abstracciones;
using Entregas.Application.Dtos;

namespace Entregas.Application.CasosDeUso;

/// <summary>Entregas propias del estudiante del token (RN-16). Admite filtrar por actividad.</summary>
public sealed class ListarMisEntregas(IEntregaRepository entregas)
{
    public async Task<IReadOnlyList<EntregaDto>> EjecutarAsync(Guid estudianteId, Guid? actividadId, CancellationToken ct) =>
        (await entregas.ListarPorEstudianteAsync(estudianteId, actividadId, ct))
            .OrderByDescending(e => e.FechaEnvio)
            .Select(EntregaDto.De)
            .ToList();
}
