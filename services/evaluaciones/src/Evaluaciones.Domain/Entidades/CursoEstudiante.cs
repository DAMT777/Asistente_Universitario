namespace Evaluaciones.Domain.Entidades;

/// <summary>Inscripción de un estudiante en un curso (datos semilla en el MVP).</summary>
public class CursoEstudiante
{
    public Guid CursoId { get; set; }
    public Guid EstudianteId { get; set; }
    public string EstudianteNombre { get; set; } = "";
    public string EstudianteCodigo { get; set; } = "";
}
