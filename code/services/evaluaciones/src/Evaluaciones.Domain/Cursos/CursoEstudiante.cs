namespace Evaluaciones.Domain.Cursos;

/// <summary>Matrícula del estudiante en el curso. En el MVP se carga con datos semilla (viene del SIAU).</summary>
public sealed class CursoEstudiante
{
    private CursoEstudiante() { }

    public CursoEstudiante(Guid cursoId, Guid estudianteId, string estudianteNombre, string estudianteCodigo)
    {
        CursoId = cursoId;
        EstudianteId = estudianteId;
        EstudianteNombre = estudianteNombre;
        EstudianteCodigo = estudianteCodigo;
    }

    public Guid CursoId { get; private set; }
    public Guid EstudianteId { get; private set; }
    public string EstudianteNombre { get; private set; } = "";
    public string EstudianteCodigo { get; private set; } = "";
}
