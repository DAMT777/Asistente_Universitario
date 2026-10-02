using Evaluaciones.Application.Abstracciones;
using Evaluaciones.Application.Dtos;
using Evaluaciones.Application.Internos;
using Evaluaciones.Domain.Errores;

namespace Evaluaciones.Application.CasosDeUso;

/// <summary>CU-09. El profesor ve el ponderado calculado de cada estudiante de su curso.</summary>
public sealed class ConsultarPonderado(
    ICursoRepository cursos,
    IInscripcionRepository inscripciones,
    IActividadRepository actividades,
    ICalificacionRepository calificaciones,
    IPublicacionCorteRepository publicaciones)
{
    /// <summary>Tabla de todos los inscritos, sin el detalle por actividad.</summary>
    public async Task<PonderadoCursoDto> PorCursoAsync(Guid profesorId, Guid cursoId, CancellationToken ct)
    {
        var curso = await AccesoCurso.ObtenerDelProfesorAsync(cursos, cursoId, profesorId, ct);
        var inscritos = await inscripciones.ListarInscritosAsync(cursoId, ct);
        var delCurso = await actividades.ListarActividadesAsync(cursoId, ct);
        var calificacionesPorEstudiante = (await calificaciones.ListarCalificacionesDelCursoAsync(cursoId, ct))
            .ToLookup(c => c.EstudianteId);
        var publicadas = await publicaciones.ListarPublicacionesDelCursoAsync(cursoId, ct);
        var publicacionesPorEstudiante = publicadas.ToLookup(p => p.EstudianteId);

        var estudiantes = inscritos
            .OrderBy(i => i.EstudianteNombre)
            .Select(i => ConstructorPonderado.Construir(
                curso, i, delCurso,
                calificacionesPorEstudiante[i.EstudianteId],
                publicacionesPorEstudiante[i.EstudianteId],
                conDetalle: false))
            .ToList();

        var resumen = publicadas
            .OrderBy(p => p.Corte)
            .Select(p => new PublicacionResumenDto(p.EstudianteId, p.Corte, p.Nota, p.FechaPublicacion))
            .ToList();

        return new PonderadoCursoDto(curso.Id, curso.Nombre, estudiantes, resumen);
    }

    /// <summary>Ponderado de un estudiante con el detalle por actividad.</summary>
    public async Task<PonderadoEstudianteDto> PorEstudianteAsync(
        Guid profesorId, Guid cursoId, Guid estudianteId, CancellationToken ct)
    {
        var curso = await AccesoCurso.ObtenerDelProfesorAsync(cursos, cursoId, profesorId, ct);
        var inscrito = (await inscripciones.ListarInscritosAsync(cursoId, ct))
                .FirstOrDefault(i => i.EstudianteId == estudianteId)
            ?? throw new DominioException(CodigosError.NoEncontrado, "El estudiante no está inscrito en el curso.");

        var delCurso = await actividades.ListarActividadesAsync(cursoId, ct);
        var suyas = (await calificaciones.ListarCalificacionesDelCursoAsync(cursoId, ct))
            .Where(c => c.EstudianteId == estudianteId);
        var publicadas = (await publicaciones.ListarPublicacionesDelCursoAsync(cursoId, ct))
            .Where(p => p.EstudianteId == estudianteId);

        return ConstructorPonderado.Construir(curso, inscrito, delCurso, suyas, publicadas, conDetalle: true);
    }
}
