using Evaluaciones.Application.Abstracciones;
using Evaluaciones.Domain.Entidades;
using Evaluaciones.Domain.Errores;

namespace Evaluaciones.Application.Internos;

/// <summary>Verificación de propiedad del curso (RN-15). Verificar solo el rol no es suficiente.</summary>
internal static class AccesoCurso
{
    public static async Task<Curso> ObtenerDelProfesorAsync(
        ICursoRepository cursos, Guid cursoId, Guid profesorId, CancellationToken ct)
    {
        var curso = await cursos.ObtenerAsync(cursoId, ct)
            ?? throw new DominioException(CodigosError.NoEncontrado, "El curso no existe.");

        if (curso.ProfesorId != profesorId)
            throw new DominioException(CodigosError.SinPermiso, "El curso pertenece a otro profesor.");

        return curso;
    }
}
