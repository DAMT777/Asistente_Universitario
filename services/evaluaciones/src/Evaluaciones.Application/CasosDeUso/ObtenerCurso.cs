using Evaluaciones.Application.Abstracciones;
using Evaluaciones.Application.Dtos;
using Evaluaciones.Application.Internos;
using Evaluaciones.Domain;
using Evaluaciones.Domain.Errores;

namespace Evaluaciones.Application.CasosDeUso;

/// <summary>GET /cursos/{cursoId}: detalle del curso con los pesos de los cortes (sección 8.4).</summary>
public sealed class ObtenerCurso(
    ICursoRepository cursos,
    IInscripcionRepository inscripciones,
    IActividadRepository actividades,
    ICalificacionRepository calificaciones)
{
    public async Task<CursoResumenDto> EjecutarAsync(Guid usuarioId, string rol, Guid cursoId, CancellationToken ct)
    {
        if (rol == Roles.Profesor)
            return (await AccesoCurso.ObtenerDelProfesorAsync(cursos, cursoId, usuarioId, ct)).AResumen();

        if (rol != Roles.Estudiante)
            throw new DominioException(CodigosError.SinPermiso, "El rol no puede consultar cursos.");

        // RN-16: para quien no está inscrito, el curso no existe.
        var curso = await inscripciones.EstaInscritoAsync(cursoId, usuarioId, ct)
            ? await cursos.ObtenerAsync(cursoId, ct)
            : null;
        if (curso is null)
            throw new DominioException(CodigosError.NoEncontrado, "El curso no existe o no estás inscrito.");

        var yaCalificadas = (await calificaciones.ListarCalificacionesPublicadasAsync(usuarioId, ct))
            .Select(c => c.ActividadId)
            .ToHashSet();
        var pendientes = (await actividades.ListarActividadesAsync(cursoId, ct)).Count(a => !yaCalificadas.Contains(a.Id));

        return curso.AResumen(pendientes);
    }
}
