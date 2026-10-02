using Evaluaciones.Application.Abstracciones;

namespace Evaluaciones.Application.Calificaciones;

/// <summary>
/// Calificaciones existentes de la actividad, borradores incluidos, solo para el profesor dueño. Quien no aparece
/// está sin calificar. Sirve para saber la versión de cada nota antes de modificarla (If-Match).
/// </summary>
public sealed class ListarCalificaciones(IActividadRepository actividades, ICalificacionRepository calificaciones)
{
    public async Task<IReadOnlyList<CalificacionDto>> EjecutarAsync(Actor actor, Guid actividadId, CancellationToken ct)
    {
        var contexto = await actividades.ExigirProfesorDuenoAsync(actor, actividadId, ct);
        var lista = await calificaciones.ListarPorActividadAsync(contexto.Actividad.Id, ct);
        return lista.Select(CalificacionDto.Desde).ToList();
    }
}
