namespace Evaluaciones.Api.Contratos;

/// <summary>Cuerpo de PUT /actividades/{id}/calificaciones/{estudianteId}. Valor es obligatorio: un 0.0 es una nota, la ausencia no.</summary>
public sealed record GuardarCalificacionSolicitud(decimal? Valor, string? Retroalimentacion, Guid? EntregaId);

/// <summary>Cuerpo opcional de POST /actividades/{id}/calificaciones/publicar. Sin cuerpo se publican todos los borradores.</summary>
public sealed record PublicarCalificacionesSolicitud(Guid[]? EstudianteIds);
