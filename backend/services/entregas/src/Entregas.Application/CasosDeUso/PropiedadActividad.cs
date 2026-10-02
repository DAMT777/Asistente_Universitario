using Entregas.Application.Abstracciones;
using Entregas.Domain;

namespace Entregas.Application.CasosDeUso;

/// <summary>RN-15: solo el profesor dueño del curso ve las entregas de sus actividades. Lo sabe Evaluaciones.</summary>
internal static class PropiedadActividad
{
    public static async Task<ActividadInfo> ExigirProfesorDuenoAsync(
        IEvaluacionesCliente evaluaciones, Guid actividadId, Guid profesorId, CancellationToken ct)
    {
        var actividad = await evaluaciones.ObtenerActividadAsync(actividadId, null, ct)
            ?? throw new DominioException(CodigosError.NoEncontrado, "La actividad no existe.");

        if (actividad.ProfesorId != profesorId)
            throw new DominioException(CodigosError.SinPermiso, "La actividad pertenece al curso de otro profesor.");

        return actividad;
    }
}
