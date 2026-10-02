using Evaluaciones.Application.Abstracciones;
using Evaluaciones.Application.Dtos;
using Evaluaciones.Application.Internos;
using Evaluaciones.Domain.Entidades;
using Evaluaciones.Domain.Errores;

namespace Evaluaciones.Application.CasosDeUso;

/// <summary>
/// CU-05, CU-06 y CU-07. El profesor dueño del curso (RN-15) registra o modifica la nota de un estudiante
/// en una actividad, con su retroalimentación.
/// <list type="bullet">
/// <item>CU-05: actividad con entrega. Se puede indicar la entrega calificada (entregaId).</item>
/// <item>CU-06: actividad sin entrega (parcial, sustentación, trabajo en clase). La nota se registra manualmente (RN-04).</item>
/// <item>CU-07: si la calificación ya existe se modifica. Si cambió algo, vuelve a BORRADOR hasta que se publique
/// de nuevo la actividad (CU-08). Si el corte ya estaba publicado, se corrige aparte (CU-11).</item>
/// </list>
/// Toda calificación nueva queda en BORRADOR y el estudiante no la ve (RN-08, RN-09).
/// </summary>
public sealed class CalificarActividad(
    ICursoRepository cursos,
    IInscripcionRepository inscripciones,
    IActividadRepository actividades,
    ICalificacionRepository calificaciones,
    IUnidadDeTrabajo unidadDeTrabajo)
{
    public async Task<CalificacionDto> EjecutarAsync(
        Guid profesorId,
        Guid actividadId,
        Guid estudianteId,
        SolicitudCalificar solicitud,
        byte[]? versionEsperada,
        CancellationToken ct)
    {
        var valor = Calificacion.ValidarValor(solicitud.Valor);
        var retroalimentacion = Calificacion.ValidarRetroalimentacion(solicitud.Retroalimentacion);

        var actividad = await actividades.ObtenerActividadAsync(actividadId, ct)
            ?? throw new DominioException(CodigosError.NoEncontrado, "La actividad no existe.");

        await AccesoCurso.ObtenerDelProfesorAsync(cursos, actividad.CursoId, profesorId, ct);

        if (!await inscripciones.EstaInscritoAsync(actividad.CursoId, estudianteId, ct))
            throw new DominioException(CodigosError.NoEncontrado, "El estudiante no está inscrito en el curso.");

        if (solicitud.EntregaId is not null && !actividad.RequiereEntrega)
            throw new DominioException(
                CodigosError.ActividadSinEntrega,
                "La actividad no requiere entrega. Registra la nota sin indicar una entrega.");

        var calificacion = await calificaciones.ObtenerCalificacionAsync(actividadId, estudianteId, ct);

        if (calificacion is null)
        {
            if (versionEsperada is not null)
                throw new DominioException(
                    CodigosError.ConflictoConcurrencia,
                    "La calificación ya no existe. Consulta de nuevo e inténtalo otra vez.");

            calificacion = Calificacion.Nueva(actividadId, estudianteId, valor, retroalimentacion, solicitud.EntregaId);
            calificaciones.AgregarCalificacion(calificacion);
        }
        else
        {
            if (versionEsperada is not null)
                calificaciones.ExigirVersion(calificacion, versionEsperada);

            if (!calificacion.Modificar(valor, retroalimentacion, solicitud.EntregaId))
                return calificacion.ADto(); // sin cambios: conserva su estado y su versión
        }

        await unidadDeTrabajo.GuardarCambiosAsync(ct);
        return calificacion.ADto();
    }
}
