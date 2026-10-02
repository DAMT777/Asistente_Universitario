using Evaluaciones.Application.Abstracciones;
using Evaluaciones.Application.Dtos;
using Evaluaciones.Domain;
using Evaluaciones.Domain.Entidades;
using Evaluaciones.Domain.Errores;

namespace Evaluaciones.Application.CasosDeUso;

/// <summary>
/// CU-12 (parte 1). Cursos del usuario según su rol. Al estudiante se le agrega cuántas actividades
/// tiene pendientes en cada uno. Pendiente significa sin calificación publicada.
/// </summary>
public sealed class ListarCursos(
    ICursoRepository cursos,
    IActividadRepository actividades,
    ICalificacionRepository calificaciones)
{
    public async Task<IReadOnlyList<CursoResumenDto>> EjecutarAsync(Guid usuarioId, string rol, CancellationToken ct)
    {
        if (rol == Roles.Profesor)
        {
            var delProfesor = await cursos.ListarPorProfesorAsync(usuarioId, ct);
            return delProfesor.Select(c => Resumen(c, null)).ToList();
        }

        if (rol == Roles.Estudiante)
        {
            var delEstudiante = await cursos.ListarPorEstudianteAsync(usuarioId, ct);
            var actividadesDeSusCursos = await actividades.ListarActividadesDeCursosAsync(
                delEstudiante.Select(c => c.Id).ToList(), ct);
            var yaCalificadas = (await calificaciones.ListarCalificacionesPublicadasAsync(usuarioId, ct))
                .Select(c => c.ActividadId)
                .ToHashSet();

            return delEstudiante
                .Select(c => Resumen(
                    c,
                    actividadesDeSusCursos.Count(a => a.CursoId == c.Id && !yaCalificadas.Contains(a.Id))))
                .ToList();
        }

        throw new DominioException(CodigosError.SinPermiso, "El rol no puede consultar cursos.");
    }

    private static CursoResumenDto Resumen(Curso curso, int? pendientes) =>
        new(curso.Id, curso.Codigo, curso.Nombre, curso.ProfesorNombre,
            curso.PesoCorte1, curso.PesoCorte2, curso.PesoCorte3, pendientes);
}
