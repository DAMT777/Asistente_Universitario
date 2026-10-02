using Evaluaciones.Application.Abstracciones;
using Evaluaciones.Application.Dtos;
using Evaluaciones.Application.Internos;
using Evaluaciones.Domain;

namespace Evaluaciones.Application.CasosDeUso;

/// <summary>
/// Apoyo de CU-09, CU-10 y CU-11: el profesor necesita la lista de inscritos de su curso para ver
/// el ponderado y publicar cortes (RN-15).
/// </summary>
public sealed class ListarEstudiantesDelCurso(ICursoRepository cursos, IInscripcionRepository inscripciones)
{
    public async Task<IReadOnlyList<EstudianteInscritoDto>> EjecutarAsync(Guid profesorId, Guid cursoId, CancellationToken ct)
    {
        await AccesoCurso.ObtenerDelProfesorAsync(cursos, cursoId, profesorId, ct);

        return (await inscripciones.ListarInscritosAsync(cursoId, ct))
            .OrderBy(i => i.EstudianteNombre)
            .Select(i => new EstudianteInscritoDto(i.EstudianteId, i.EstudianteNombre, i.EstudianteCodigo, Roles.Estudiante))
            .ToList();
    }
}
