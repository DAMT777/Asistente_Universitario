using Evaluaciones.Domain.Entidades;

namespace Evaluaciones.Application.Dtos;

/// <summary>Cuerpo de PUT /actividades/{actividadId}/calificaciones/{estudianteId} (CU-05, CU-06, CU-07).</summary>
/// <param name="Valor">Nota de 0.0 a 5.0 con un decimal como máximo (RN-03). Obligatoria.</param>
/// <param name="Retroalimentacion">Comentario para el estudiante. Opcional, hasta 2000 caracteres.</param>
/// <param name="EntregaId">Entrega que se califica (CU-05). Solo para actividades que requieren entrega.</param>
public sealed record SolicitudCalificar(
    decimal? Valor,
    string? Retroalimentacion = null,
    Guid? EntregaId = null);

/// <param name="Publicadas">Cuántos borradores pasaron a PUBLICADA. 0 si no había ninguno.</param>
public sealed record ResultadoPublicacionCalificaciones(Guid ActividadId, int Publicadas);

internal static class MapeoCalificacion
{
    public static CalificacionDto ADto(this Calificacion c) =>
        new(
            c.Id,
            c.ActividadId,
            c.EstudianteId,
            c.EntregaId,
            c.Valor,
            c.Retroalimentacion ?? "",
            c.Estado.ComoTexto(),
            c.Version is null ? null : Convert.ToBase64String(c.Version));
}
