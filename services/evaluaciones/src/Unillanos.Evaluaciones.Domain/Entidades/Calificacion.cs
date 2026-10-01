namespace Unillanos.Evaluaciones.Domain.Entidades;

public enum EstadoCalificacion
{
    BORRADOR,
    PUBLICADA,
}

/// <summary>RN-08: una calificación puede no existir (sin calificar), estar en BORRADOR o PUBLICADA.</summary>
public sealed class Calificacion
{
    public required Guid Id { get; init; }
    public required Guid ActividadId { get; init; }
    public required Guid EstudianteId { get; init; }
    /// <summary>Referencia a Entregas (otro servicio): solo el id, sin llave foránea.</summary>
    public Guid? EntregaId { get; init; }
    /// <summary>0..5.</summary>
    public required decimal Valor { get; init; }
    public string? Retroalimentacion { get; init; }
    public required EstadoCalificacion Estado { get; init; }
    public required int Version { get; init; }

    public bool EsVisibleParaEstudiante => Estado == EstadoCalificacion.PUBLICADA;
}

/// <summary>Nota publicada de un corte para un estudiante (una fila por versión publicada).</summary>
public sealed class PublicacionCorte
{
    public required Guid Id { get; init; }
    public required Guid CursoId { get; init; }
    public required Guid EstudianteId { get; init; }
    public required int Corte { get; init; }
    public required decimal Nota { get; init; }
    public required DateTime FechaPublicacion { get; init; }
    public required int Version { get; init; }
}
