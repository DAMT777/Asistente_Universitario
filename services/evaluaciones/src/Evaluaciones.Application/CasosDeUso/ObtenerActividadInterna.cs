using Evaluaciones.Application.Puertos;

namespace Evaluaciones.Application.CasosDeUso;

public sealed record ActividadInternaRespuesta(
    Guid ActividadId,
    Guid CursoId,
    Guid ProfesorId,
    DateTime FechaLimite,
    bool RequiereEntrega,
    bool EstudianteInscrito);

/// <summary>Consulta servicio a servicio usada por Entregas. Devuelve null si la actividad no existe.</summary>
public sealed class ObtenerActividadInterna(IActividadRepositorio actividades, ICursoRepositorio cursos)
{
    public async Task<ActividadInternaRespuesta?> EjecutarAsync(Guid actividadId, Guid estudianteId, CancellationToken ct)
    {
        var actividad = await actividades.ObtenerAsync(actividadId, ct);
        if (actividad is null) return null;

        var curso = await cursos.ObtenerAsync(actividad.CursoId, ct);
        if (curso is null) return null;

        var inscrito = await cursos.EstaInscritoAsync(curso.Id, estudianteId, ct);
        return new ActividadInternaRespuesta(
            actividad.Id,
            curso.Id,
            curso.ProfesorId,
            DateTime.SpecifyKind(actividad.FechaLimite, DateTimeKind.Utc),
            actividad.RequiereEntrega,
            inscrito);
    }
}
