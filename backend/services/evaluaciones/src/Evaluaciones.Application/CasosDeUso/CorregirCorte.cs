using Evaluaciones.Application.Abstracciones;
using Evaluaciones.Application.Dtos;
using Evaluaciones.Application.Internos;
using Evaluaciones.Domain.Errores;
using Evaluaciones.Domain.Ponderado;

namespace Evaluaciones.Application.CasosDeUso;

/// <summary>
/// CU-11. Recalcula y actualiza solo la publicación de un estudiante en un corte ya publicado (RN-13).
/// Aplica la misma regla de borradores que la publicación (RN-12): si el profesor modificó una
/// calificación y quedó en borrador, primero debe publicar la actividad (CU-08).
/// </summary>
public sealed class CorregirCorte(
    ICursoRepository cursos,
    IActividadRepository actividades,
    ICalificacionRepository calificaciones,
    IPublicacionCorteRepository publicaciones,
    IUnidadDeTrabajo unidadDeTrabajo,
    TimeProvider reloj)
{
    public async Task<PublicacionCorteDto> EjecutarAsync(
        Guid profesorId,
        Guid cursoId,
        int corte,
        Guid estudianteId,
        SolicitudCorregirCorte solicitud,
        byte[]? versionEsperada,
        CancellationToken ct)
    {
        if (!MotorPonderado.EsCorteValido(corte))
            throw new DominioException(CodigosError.ValidacionFallida, "El corte debe ser 1, 2 o 3.");

        await AccesoCurso.ObtenerDelProfesorAsync(cursos, cursoId, profesorId, ct);

        var publicacion = await publicaciones.ObtenerPublicacionAsync(cursoId, estudianteId, corte, ct)
            ?? throw new DominioException(
                CodigosError.NoEncontrado, "El corte aún no está publicado para el estudiante.");

        var actividadesDelCorte = (await actividades.ListarActividadesAsync(cursoId, ct))
            .Where(a => a.Corte == corte)
            .ToList();
        var suyas = (await calificaciones.ListarCalificacionesDelCursoAsync(cursoId, ct))
            .Where(c => c.EstudianteId == estudianteId);

        var nota = MotorPonderado.NotaCortePublicable(actividadesDelCorte, suyas, solicitud.OmitirBorradores);

        if (versionEsperada is not null)
            publicaciones.ExigirVersion(publicacion, versionEsperada);

        publicacion.Actualizar(nota, reloj.GetUtcNow().UtcDateTime);
        await unidadDeTrabajo.GuardarCambiosAsync(ct);

        return new PublicacionCorteDto(
            publicacion.EstudianteId,
            publicacion.Corte,
            publicacion.Nota,
            publicacion.FechaPublicacion,
            publicacion.Version is null ? null : Convert.ToBase64String(publicacion.Version));
    }
}
