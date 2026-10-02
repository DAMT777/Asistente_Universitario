using Evaluaciones.Application.Abstracciones;

namespace Evaluaciones.Application.Calificaciones;

/// <param name="EstudianteIds">Opcional. Nulo publica todos los borradores de la actividad; con ids, solo los de esos estudiantes.</param>
public sealed record PublicarCalificacionesComando(Actor Actor, Guid ActividadId, IReadOnlyCollection<Guid>? EstudianteIds);

public sealed record ResultadoPublicacion(Guid ActividadId, int Publicadas);

/// <summary>
/// CU-08. Habilitar una nota para el estudiante es publicar las calificaciones de la actividad: pasan de
/// borrador a publicada, todas o ninguna. Es idempotente: sin borradores devuelve 0.
/// </summary>
public sealed class PublicarCalificaciones(
    IActividadRepository actividades,
    ICalificacionRepository calificaciones,
    IUnidadDeTrabajo unidad)
{
    public async Task<ResultadoPublicacion> EjecutarAsync(PublicarCalificacionesComando cmd, CancellationToken ct)
    {
        var contexto = await actividades.ExigirProfesorDuenoAsync(cmd.Actor, cmd.ActividadId, ct);

        var borradores = await calificaciones.ListarBorradoresAsync(contexto.Actividad.Id, cmd.EstudianteIds, ct);
        var publicadas = borradores.Count(c => c.Publicar());

        if (publicadas > 0)
            await unidad.GuardarAsync(ct);

        return new ResultadoPublicacion(contexto.Actividad.Id, publicadas);
    }
}
