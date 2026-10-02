namespace Evaluaciones.Domain.Entidades;

public class Actividad
{
    public Guid Id { get; set; }
    public Guid CursoId { get; set; }
    public string Titulo { get; set; } = "";
    public int Corte { get; set; }

    /// <summary>Porcentaje de 0 a 100 dentro de su corte (DA-03).</summary>
    public decimal Peso { get; set; }

    /// <summary>En UTC (DA-10).</summary>
    public DateTime? FechaLimite { get; set; }

    public bool RequiereEntrega { get; set; }
}
