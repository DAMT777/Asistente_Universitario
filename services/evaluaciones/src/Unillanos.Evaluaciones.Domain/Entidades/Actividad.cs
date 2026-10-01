namespace Unillanos.Evaluaciones.Domain.Entidades;

public sealed class Actividad
{
    public required Guid Id { get; init; }
    public required Guid CursoId { get; init; }
    public required string Titulo { get; init; }
    /// <summary>Corte 1..3.</summary>
    public required int Corte { get; init; }
    /// <summary>Peso dentro de su corte (las actividades de un corte suman 100).</summary>
    public required decimal Peso { get; init; }
    /// <summary>Fecha límite en UTC.</summary>
    public required DateTime FechaLimite { get; init; }
    public required bool RequiereEntrega { get; init; }
}
