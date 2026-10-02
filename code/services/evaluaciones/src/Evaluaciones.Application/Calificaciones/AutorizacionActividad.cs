using Evaluaciones.Application.Abstracciones;
using Evaluaciones.Domain;

namespace Evaluaciones.Application.Calificaciones;

internal static class AutorizacionActividad
{
    /// <summary>Exige rol de profesor y que sea el dueño del curso de la actividad (RN-15). Verificar solo el rol no basta.</summary>
    public static async Task<ContextoActividad> ExigirProfesorDuenoAsync(
        this IActividadRepository actividades, Actor actor, Guid actividadId, CancellationToken ct)
    {
        actor.ExigirProfesor();
        var contexto = await actividades.ObtenerContextoAsync(actividadId, ct)
            ?? throw new NoEncontradoException("La actividad no existe.");
        if (contexto.ProfesorId != actor.Id)
            throw new SinPermisoException("Solo el profesor dueño del curso puede calificar esta actividad.");
        return contexto;
    }
}
