using Entregas.Application.Comun;
using Entregas.Application.Puertos;

namespace Entregas.Application.CasosDeUso;

/// <summary>
/// Entregas de todos los estudiantes en una actividad, para la pantalla de calificar del profesor.
/// Solo el profesor dueño del curso (RN-15); el dato lo confirma Evaluaciones.
/// </summary>
public sealed class ListarEntregasDeActividad(ConsultorActividades actividades, IEntregaRepositorio repositorio)
{
    public async Task<IReadOnlyList<EntregaRespuesta>> EjecutarAsync(Guid actividadId, Guid profesorId, CancellationToken ct)
    {
        await actividades.ObtenerParaProfesorAsync(actividadId, profesorId, ct);
        var entregas = await repositorio.ListarDeActividadAsync(actividadId, ct);
        return entregas.Select(EntregaRespuesta.Desde).ToList();
    }
}
