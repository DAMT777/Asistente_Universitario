namespace Evaluaciones.Domain.Entidades;

/// <summary>
/// Nota de un estudiante en una actividad. Si no existe la fila, la actividad está sin calificar,
/// y eso es distinto de una calificación con valor 0 (RN-08).
/// </summary>
public class Calificacion
{
    public Guid Id { get; set; }
    public Guid ActividadId { get; set; }
    public Guid EstudianteId { get; set; }
    public Guid? EntregaId { get; set; }
    public decimal Valor { get; set; }
    public string? Retroalimentacion { get; set; }
    public EstadoCalificacion Estado { get; set; }
    public byte[]? Version { get; set; }
}
