namespace Evaluaciones.Domain.Entidades;

/// <summary>Nota de un corte publicada por el profesor para un estudiante.</summary>
public class PublicacionCorte
{
    public Guid Id { get; set; }
    public Guid CursoId { get; set; }
    public Guid EstudianteId { get; set; }
    public int Corte { get; set; }
    public decimal Nota { get; set; }

    /// <summary>En UTC.</summary>
    public DateTime FechaPublicacion { get; set; }

    public byte[]? Version { get; set; }

    public void Actualizar(decimal nota, DateTime fechaPublicacion)
    {
        Nota = nota;
        FechaPublicacion = fechaPublicacion;
    }
}
