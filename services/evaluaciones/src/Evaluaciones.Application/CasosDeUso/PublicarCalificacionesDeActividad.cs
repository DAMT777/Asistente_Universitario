using Evaluaciones.Application.Abstracciones;
using Evaluaciones.Application.Dtos;
using Evaluaciones.Application.Internos;
using Evaluaciones.Domain.Errores;

namespace Evaluaciones.Application.CasosDeUso;

/// <summary>
/// CU-08. El profesor dueño del curso (RN-15) publica todas las calificaciones en borrador de una actividad.
/// Pasan a PUBLICADA y el estudiante ya las ve con su retroalimentación (RN-09). Publicar la actividad no
/// publica el corte: eso es CU-10. Si no hay borradores no hace nada y responde 0.
/// </summary>
public sealed class PublicarCalificacionesDeActividad(
    ICursoRepository cursos,
    IActividadRepository actividades,
    ICalificacionRepository calificaciones,
    IUnidadDeTrabajo unidadDeTrabajo)
{
    public async Task<ResultadoPublicacionCalificaciones> EjecutarAsync(
        Guid profesorId, Guid actividadId, CancellationToken ct)
    {
        var actividad = await actividades.ObtenerActividadAsync(actividadId, ct)
            ?? throw new DominioException(CodigosError.NoEncontrado, "La actividad no existe.");

        await AccesoCurso.ObtenerDelProfesorAsync(cursos, actividad.CursoId, profesorId, ct);

        var borradores = await calificaciones.ListarBorradoresDeActividadAsync(actividadId, ct);
        var publicadas = borradores.Count(c => c.Publicar());

        if (publicadas > 0)
            await unidadDeTrabajo.GuardarCambiosAsync(ct);

        return new ResultadoPublicacionCalificaciones(actividadId, publicadas);
    }
}
