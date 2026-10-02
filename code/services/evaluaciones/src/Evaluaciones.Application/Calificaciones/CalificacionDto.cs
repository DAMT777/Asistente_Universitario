using Evaluaciones.Domain.Calificaciones;

namespace Evaluaciones.Application.Calificaciones;

/// <summary>Forma de la calificación en la API (guía técnica, sección 8.7).</summary>
public sealed record CalificacionDto(
    Guid Id,
    Guid ActividadId,
    Guid EstudianteId,
    Guid? EntregaId,
    decimal Valor,
    string? Retroalimentacion,
    string Estado,
    string Version)
{
    public static CalificacionDto Desde(Calificacion c) => new(
        c.Id,
        c.ActividadId,
        c.EstudianteId,
        c.EntregaId,
        c.Valor,
        c.Retroalimentacion,
        c.Estado == EstadoCalificacion.Publicada ? "PUBLICADA" : "BORRADOR",
        Convert.ToBase64String(c.Version));
}
