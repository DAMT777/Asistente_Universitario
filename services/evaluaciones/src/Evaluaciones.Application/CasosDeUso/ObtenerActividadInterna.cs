using Evaluaciones.Application.Abstracciones;
using Evaluaciones.Application.Dtos;
using Evaluaciones.Domain.Errores;

namespace Evaluaciones.Application.CasosDeUso;

/// <summary>
/// Consulta servicio a servicio que usa Entregas para validar una entrega (fecha límite, si requiere entrega,
/// inscripción) y para saber quién es el profesor dueño. Sin estudiante, EstudianteInscrito es false.
/// </summary>
public sealed class ObtenerActividadInterna(
    IActividadRepository actividades,
    ICursoRepository cursos,
    IInscripcionRepository inscripciones)
{
    public async Task<ActividadInternaDto> EjecutarAsync(Guid actividadId, Guid? estudianteId, CancellationToken ct)
    {
        var actividad = await actividades.ObtenerActividadAsync(actividadId, ct)
            ?? throw new DominioException(CodigosError.NoEncontrado, "La actividad no existe.");
        var curso = await cursos.ObtenerAsync(actividad.CursoId, ct)
            ?? throw new DominioException(CodigosError.NoEncontrado, "La actividad no existe.");

        var inscrito = estudianteId is { } id && await inscripciones.EstaInscritoAsync(curso.Id, id, ct);
        return new ActividadInternaDto(actividad.Id, curso.Id, curso.ProfesorId, actividad.FechaLimite, actividad.RequiereEntrega, inscrito);
    }
}
