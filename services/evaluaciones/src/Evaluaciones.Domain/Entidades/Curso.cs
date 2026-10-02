namespace Evaluaciones.Domain.Entidades;

public class Curso
{
    public Guid Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public Guid ProfesorId { get; set; }
    public string ProfesorNombre { get; set; } = "";
    public decimal PesoCorte1 { get; set; }
    public decimal PesoCorte2 { get; set; }
    public decimal PesoCorte3 { get; set; }

    public decimal PesoDeCorte(int corte) => corte switch
    {
        1 => PesoCorte1,
        2 => PesoCorte2,
        3 => PesoCorte3,
        _ => throw new ArgumentOutOfRangeException(nameof(corte), "El corte debe ser 1, 2 o 3.")
    };
}
