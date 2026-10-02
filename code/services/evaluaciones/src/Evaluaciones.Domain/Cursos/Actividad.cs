namespace Evaluaciones.Domain.Cursos;

public sealed class Actividad
{
    private Actividad() { }

    public Actividad(Guid id, Guid cursoId, string titulo, int corte, decimal peso, DateTime? fechaLimite, bool requiereEntrega)
    {
        Id = id;
        CursoId = cursoId;
        Titulo = titulo;
        Corte = corte;
        Peso = peso;
        FechaLimite = fechaLimite;
        RequiereEntrega = requiereEntrega;
    }

    public Guid Id { get; private set; }
    public Guid CursoId { get; private set; }
    public string Titulo { get; private set; } = "";
    public int Corte { get; private set; }
    /// <summary>Porcentaje (0-100) dentro de su corte.</summary>
    public decimal Peso { get; private set; }
    public DateTime? FechaLimite { get; private set; }
    /// <summary>true: tarea con archivo. false: parcial o sustentación, la nota se registra a mano (RN-04).</summary>
    public bool RequiereEntrega { get; private set; }
}
