namespace Evaluaciones.Domain.Cursos;

public sealed class Curso
{
    private Curso() { }

    public Curso(Guid id, string codigo, string nombre, Guid profesorId, string profesorNombre,
                 decimal pesoCorte1, decimal pesoCorte2, decimal pesoCorte3)
    {
        Id = id;
        Codigo = codigo;
        Nombre = nombre;
        ProfesorId = profesorId;
        ProfesorNombre = profesorNombre;
        PesoCorte1 = pesoCorte1;
        PesoCorte2 = pesoCorte2;
        PesoCorte3 = pesoCorte3;
    }

    public Guid Id { get; private set; }
    public string Codigo { get; private set; } = "";
    public string Nombre { get; private set; } = "";
    /// <summary>Referencia al servicio de usuarios, sin llave foránea.</summary>
    public Guid ProfesorId { get; private set; }
    public string ProfesorNombre { get; private set; } = "";
    public decimal PesoCorte1 { get; private set; }
    public decimal PesoCorte2 { get; private set; }
    public decimal PesoCorte3 { get; private set; }
}
