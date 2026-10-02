using Evaluaciones.Application.Abstracciones;
using Evaluaciones.Domain;
using Evaluaciones.Domain.Calificaciones;

namespace Evaluaciones.Application.Calificaciones;

/// <param name="Valor">Nullable a propósito: un valor ausente es un error, nunca un 0 implícito (RN-08).</param>
/// <param name="EntregaId">Entrega que se está calificando (CU-05). Nulo en actividades sin entrega (CU-06) o para conservar el vínculo actual (CU-07).</param>
/// <param name="VersionEsperada">Versión que el cliente leyó (If-Match). Si no coincide, 409.</param>
public sealed record GuardarCalificacionComando(
    Actor Actor,
    Guid ActividadId,
    Guid EstudianteId,
    decimal? Valor,
    string? Retroalimentacion,
    Guid? EntregaId,
    string? VersionEsperada);

/// <summary>
/// CU-05, CU-06 y CU-07. Crea la calificación si no existe (borrador) o la modifica si ya existe, en cuyo caso
/// vuelve a borrador hasta que el profesor publique de nuevo la actividad.
/// </summary>
public sealed class GuardarCalificacion(
    IActividadRepository actividades,
    ICalificacionRepository calificaciones,
    IUnidadDeTrabajo unidad)
{
    public async Task<CalificacionDto> EjecutarAsync(GuardarCalificacionComando cmd, CancellationToken ct)
    {
        cmd.Actor.ExigirProfesor();
        if (cmd.Valor is null)
            throw new ValidacionFallidaException("El campo valor es obligatorio. Para dejar una actividad sin calificar no se envía ninguna nota.");

        var contexto = await actividades.ExigirProfesorDuenoAsync(cmd.Actor, cmd.ActividadId, ct);
        var actividad = contexto.Actividad;

        if (!await actividades.EstudianteInscritoAsync(actividad.CursoId, cmd.EstudianteId, ct))
            throw new NoEncontradoException("El estudiante no está inscrito en el curso de esta actividad.");

        var existente = await calificaciones.ObtenerAsync(actividad.Id, cmd.EstudianteId, ct);
        Calificacion resultado;

        if (existente is null)
        {
            if (cmd.VersionEsperada is not null)
                throw new ConflictoConcurrenciaException("La calificación que intentas modificar ya no existe.");

            resultado = Calificacion.Crear(Guid.NewGuid(), actividad, cmd.EstudianteId, cmd.EntregaId, cmd.Valor.Value, cmd.Retroalimentacion);
            calificaciones.Agregar(resultado);
        }
        else
        {
            if (cmd.VersionEsperada is not null && cmd.VersionEsperada != Convert.ToBase64String(existente.Version))
                throw new ConflictoConcurrenciaException();

            existente.Modificar(actividad, cmd.EntregaId, cmd.Valor.Value, cmd.Retroalimentacion);
            resultado = existente;
        }

        await unidad.GuardarAsync(ct);
        return CalificacionDto.Desde(resultado);
    }
}
