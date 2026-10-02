using Evaluaciones.Application.Abstracciones;
using Evaluaciones.Application.Dtos;
using Evaluaciones.Application.Internos;
using Evaluaciones.Domain.Errores;

namespace Evaluaciones.Application.CasosDeUso;

/// <summary>
/// CU-02. El profesor dueño del curso (RN-15) define el peso de los tres cortes, que deben sumar 100 (RN-01).
/// El ponderado se calcula al consultar, así que el cambio se refleja de inmediato en la definitiva.
/// </summary>
public sealed class DefinirPesosCortes(ICursoRepository cursos, IUnidadDeTrabajo unidadDeTrabajo)
{
    public async Task<CursoResumenDto> EjecutarAsync(
        Guid profesorId, Guid cursoId, SolicitudPesosCortes solicitud, CancellationToken ct)
    {
        await AccesoCurso.ObtenerDelProfesorAsync(cursos, cursoId, profesorId, ct);

        var curso = await cursos.ObtenerCursoParaEditarAsync(cursoId, ct)
            ?? throw new DominioException(CodigosError.NoEncontrado, "El curso no existe.");

        curso.DefinirPesos(solicitud.PesoCorte1, solicitud.PesoCorte2, solicitud.PesoCorte3);
        await unidadDeTrabajo.GuardarCambiosAsync(ct);

        return curso.AResumen();
    }
}
