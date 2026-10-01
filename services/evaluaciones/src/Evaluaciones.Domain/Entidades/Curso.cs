namespace Evaluaciones.Domain.Entidades;

public sealed class Curso
{
    public required Guid Id { get; init; }
    public required string Codigo { get; init; }
    public required string Nombre { get; init; }
    public required Guid ProfesorId { get; init; }
    public required string ProfesorNombre { get; init; }
    public required decimal PesoCorte1 { get; init; }
    public required decimal PesoCorte2 { get; init; }
    public required decimal PesoCorte3 { get; init; }

    public const int NumeroCortes = 3;

    public decimal PesoDe(int corte) => corte switch
    {
        1 => PesoCorte1,
        2 => PesoCorte2,
        3 => PesoCorte3,
        _ => throw new ArgumentOutOfRangeException(nameof(corte), corte, "El corte debe estar entre 1 y 3."),
    };
}

public sealed class CursoEstudiante
{
    public required Guid CursoId { get; init; }
    public required Guid EstudianteId { get; init; }
}
