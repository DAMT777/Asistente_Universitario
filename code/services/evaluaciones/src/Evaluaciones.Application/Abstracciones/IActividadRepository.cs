using Evaluaciones.Domain.Cursos;

namespace Evaluaciones.Application.Abstracciones;

/// <summary>La actividad junto con el dueño de su curso, para verificar la propiedad (RN-15).</summary>
public sealed record ContextoActividad(Actividad Actividad, Guid ProfesorId);

public interface IActividadRepository
{
    Task<ContextoActividad?> ObtenerContextoAsync(Guid actividadId, CancellationToken ct);
    Task<bool> EstudianteInscritoAsync(Guid cursoId, Guid estudianteId, CancellationToken ct);
}
