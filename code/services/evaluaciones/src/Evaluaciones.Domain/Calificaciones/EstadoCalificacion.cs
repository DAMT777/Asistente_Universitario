namespace Evaluaciones.Domain.Calificaciones;

/// <summary>
/// "Sin calificar" no es un estado: significa que la fila no existe (RN-08).
/// Borrador solo lo ve el profesor; Publicada la ve el estudiante.
/// </summary>
public enum EstadoCalificacion
{
    Borrador,
    Publicada,
}
