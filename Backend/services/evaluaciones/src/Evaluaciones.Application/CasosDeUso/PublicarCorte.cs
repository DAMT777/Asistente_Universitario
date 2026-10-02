using Evaluaciones.Application.Abstracciones;
using Evaluaciones.Application.Dtos;
using Evaluaciones.Application.Internos;
using Evaluaciones.Domain.Entidades;
using Evaluaciones.Domain.Errores;
using Evaluaciones.Domain.Ponderado;

namespace Evaluaciones.Application.CasosDeUso;

/// <summary>
/// CU-10. El profesor publica la nota de un corte por estudiante. Un rechazo no impide publicar a los demás:
/// el resultado informa estudiante por estudiante. Publicar de nuevo actualiza la publicación existente.
/// </summary>
public sealed class PublicarCorte(
    ICursoRepository cursos,
    IInscripcionRepository inscripciones,
    IActividadRepository actividades,
    ICalificacionRepository calificaciones,
    IPublicacionCorteRepository publicaciones,
    IUnidadDeTrabajo unidadDeTrabajo,
    TimeProvider reloj)
{
    public async Task<ResultadoPublicacionCorte> EjecutarAsync(
        Guid profesorId, Guid cursoId, int corte, SolicitudPublicarCorte solicitud, CancellationToken ct)
    {
        if (!MotorPonderado.EsCorteValido(corte))
            throw new DominioException(CodigosError.ValidacionFallida, "El corte debe ser 1, 2 o 3.");

        await AccesoCurso.ObtenerDelProfesorAsync(cursos, cursoId, profesorId, ct);

        var inscritos = (await inscripciones.ListarInscritosAsync(cursoId, ct)).ToDictionary(i => i.EstudianteId);
        var actividadesDelCorte = (await actividades.ListarActividadesAsync(cursoId, ct))
            .Where(a => a.Corte == corte)
            .ToList();
        var calificacionesPorEstudiante = (await calificaciones.ListarCalificacionesDelCursoAsync(cursoId, ct))
            .ToLookup(c => c.EstudianteId);
        var existentes = (await publicaciones.ListarPublicacionesDelCursoAsync(cursoId, ct))
            .Where(p => p.Corte == corte)
            .ToDictionary(p => p.EstudianteId);

        var objetivo = solicitud.Estudiantes is null
            ? inscritos.Keys.ToList()
            : solicitud.Estudiantes.Distinct().ToList();
        var ahora = reloj.GetUtcNow().UtcDateTime;

        var publicados = new List<CortePublicadoDto>();
        var rechazados = new List<CorteRechazadoDto>();

        foreach (var estudianteId in objetivo)
        {
            if (!inscritos.ContainsKey(estudianteId))
            {
                rechazados.Add(new(estudianteId, CodigosError.NoEncontrado, "El estudiante no está inscrito en el curso."));
                continue;
            }

            try
            {
                var nota = MotorPonderado.NotaCortePublicable(
                    actividadesDelCorte, calificacionesPorEstudiante[estudianteId], solicitud.OmitirBorradores);

                if (existentes.TryGetValue(estudianteId, out var existente))
                    existente.Actualizar(nota, ahora);
                else
                    publicaciones.AgregarPublicacion(new PublicacionCorte
                    {
                        Id = Guid.NewGuid(),
                        CursoId = cursoId,
                        EstudianteId = estudianteId,
                        Corte = corte,
                        Nota = nota,
                        FechaPublicacion = ahora
                    });

                publicados.Add(new(estudianteId, nota));
            }
            catch (DominioException ex)
            {
                rechazados.Add(new(estudianteId, ex.Codigo, ex.Message));
            }
        }

        await unidadDeTrabajo.GuardarCambiosAsync(ct);
        return new ResultadoPublicacionCorte(corte, publicados, rechazados);
    }
}
