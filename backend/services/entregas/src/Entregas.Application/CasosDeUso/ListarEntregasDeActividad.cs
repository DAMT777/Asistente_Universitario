using Entregas.Application.Abstracciones;
using Entregas.Application.Dtos;

namespace Entregas.Application.CasosDeUso;

/// <summary>
/// CU-04. El profesor dueño del curso (RN-15) ve las entregas de una actividad, la más reciente primero.
/// Incluye las anuladas con su estado, para que el profesor sepa que existieron. El frontend combina esta
/// lista con las calificaciones de Evaluaciones.
/// </summary>
public sealed class ListarEntregasDeActividad(IEntregaRepository entregas, IEvaluacionesCliente evaluaciones)
{
    public async Task<IReadOnlyList<EntregaDto>> EjecutarAsync(Guid profesorId, Guid actividadId, CancellationToken ct)
    {
        await PropiedadActividad.ExigirProfesorDuenoAsync(evaluaciones, actividadId, profesorId, ct);

        return (await entregas.ListarPorActividadAsync(actividadId, ct))
            .OrderByDescending(e => e.FechaEnvio)
            .Select(EntregaDto.De)
            .ToList();
    }
}
