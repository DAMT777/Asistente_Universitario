using Evaluaciones.Application.Abstracciones;
using Evaluaciones.Application.Dtos;
using Evaluaciones.Domain.Errores;

namespace Evaluaciones.Application.CasosDeUso;

/// <summary>
/// Endpoint interno de la sección 8.6. Solo lo usa el servicio de entregas, para verificar la propiedad del
/// curso (CU-04, RN-15) y, al recibir una entrega, la fecha límite y la inscripción (CU-13).
/// </summary>
public sealed class ConsultarActividadInterna(
    ICursoRepository cursos,
    IInscripcionRepository inscripciones,
    IActividadRepository actividades)
{
    public async Task<ActividadInternaDto> EjecutarAsync(Guid actividadId, Guid? estudianteId, CancellationToken ct)
    {
        var actividad = await actividades.ObtenerActividadAsync(actividadId, ct)
            ?? throw new DominioException(CodigosError.NoEncontrado, "La actividad no existe.");
        var curso = await cursos.ObtenerAsync(actividad.CursoId, ct)
            ?? throw new DominioException(CodigosError.NoEncontrado, "El curso de la actividad no existe.");

        bool? inscrito = estudianteId is null
            ? null
            : await inscripciones.EstaInscritoAsync(curso.Id, estudianteId.Value, ct);

        return new ActividadInternaDto(
            actividad.Id, curso.Id, curso.ProfesorId, actividad.FechaLimite, actividad.RequiereEntrega, inscrito);
    }
}
