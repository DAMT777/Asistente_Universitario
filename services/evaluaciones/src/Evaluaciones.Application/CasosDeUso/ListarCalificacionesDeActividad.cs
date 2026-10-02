using Evaluaciones.Application.Abstracciones;
using Evaluaciones.Application.Dtos;
using Evaluaciones.Application.Internos;
using Evaluaciones.Domain.Entidades;
using Evaluaciones.Domain.Errores;

namespace Evaluaciones.Application.CasosDeUso;

/// <summary>
/// Apoyo de CU-09, CU-10 y CU-11: lectura de las calificaciones de una actividad, en cualquier estado,
/// solo para el profesor dueño del curso. Las actividades sin fila siguen sin calificar (RN-08).
/// </summary>
public sealed class ListarCalificacionesDeActividad(
    ICursoRepository cursos,
    IActividadRepository actividades,
    ICalificacionRepository calificaciones)
{
    public async Task<IReadOnlyList<CalificacionDto>> EjecutarAsync(Guid profesorId, Guid actividadId, CancellationToken ct)
    {
        var actividad = await actividades.ObtenerActividadAsync(actividadId, ct)
            ?? throw new DominioException(CodigosError.NoEncontrado, "La actividad no existe.");

        await AccesoCurso.ObtenerDelProfesorAsync(cursos, actividad.CursoId, profesorId, ct);

        return (await calificaciones.ListarCalificacionesDelCursoAsync(actividad.CursoId, ct))
            .Where(c => c.ActividadId == actividadId)
            .Select(c => new CalificacionDto(
                c.ActividadId,
                c.EstudianteId,
                c.EntregaId,
                c.Valor,
                c.Retroalimentacion,
                c.Estado.ComoTexto(),
                c.Version is null ? null : Convert.ToBase64String(c.Version)))
            .ToList();
    }
}
