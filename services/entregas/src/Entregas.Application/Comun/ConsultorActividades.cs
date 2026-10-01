using Entregas.Application.Puertos;
using Entregas.Domain;

namespace Entregas.Application.Comun;

/// <summary>Obtiene la actividad desde Evaluaciones y aplica RN-16 (existe y el estudiante está inscrito).</summary>
public sealed class ConsultorActividades(IEvaluacionesClient evaluaciones)
{
    public async Task<ActividadInfo> ObtenerParaEstudianteAsync(Guid actividadId, Guid estudianteId, CancellationToken ct)
    {
        var actividad = await evaluaciones.ObtenerActividadAsync(actividadId, estudianteId, ct)
                        ?? throw new NoEncontradoException("La actividad");
        if (!actividad.EstudianteInscrito)
            throw new SinPermisoException("El estudiante no está inscrito en el curso de la actividad.");
        return actividad;
    }
}
