using System.Text.Json;

namespace Asistente.Application.Puertos;

/// <summary>
/// Lecturas y escrituras de Evaluaciones y Entregas, SIEMPRE a través del gateway y con el JWT del usuario.
/// El asistente no tiene credenciales propias: solo puede hacer lo que el usuario podría hacer por sí mismo.
/// Los errores del servicio llegan como <see cref="ErrorRemotoException"/> con su estado y código.
/// </summary>
public interface IGatewayCliente
{
    // Estudiante
    Task<MatrizNotas> MisNotasAsync(CancellationToken ct);
    Task<NotasCurso> MisNotasDeCursoAsync(Guid cursoId, CancellationToken ct);
    Task<IReadOnlyList<EntregaPropia>> MisEntregasAsync(CancellationToken ct);

    // Profesor
    Task<IReadOnlyList<CursoProfesor>> CursosAsync(CancellationToken ct);
    Task<IReadOnlyList<EstudianteInscrito>> EstudiantesDelCursoAsync(Guid cursoId, CancellationToken ct);
    Task<IReadOnlyList<ActividadCurso>> ActividadesDelCursoAsync(Guid cursoId, CancellationToken ct);
    Task<IReadOnlyList<CalificacionActividad>> CalificacionesDeActividadAsync(Guid actividadId, CancellationToken ct);

    /// <summary>PUT de la calificación en borrador. Nunca se reintenta.</summary>
    Task<JsonElement> CalificarAsync(Guid actividadId, Guid estudianteId, decimal valor, string? retroalimentacion, string? versionEsperada, CancellationToken ct);
}

public sealed class ErrorRemotoException(int status, string codigo, string mensaje) : Exception(mensaje)
{
    public int Status { get; } = status;
    public string Codigo { get; } = codigo;
}

public sealed record CorteMatriz(int Corte, decimal PesoCorte, decimal? Nota, bool Publicado);
public sealed record CursoMatriz(Guid CursoId, string Codigo, string Nombre, string Profesor, IReadOnlyList<CorteMatriz> Cortes, decimal DefinitivaParcial, bool EsParcial);
public sealed record MatrizNotas(IReadOnlyList<CursoMatriz> Cursos);
public sealed record NotaActividad(Guid ActividadId, string Titulo, int Corte, decimal Peso, DateTime? FechaLimite, string Estado, decimal? Nota, string? Retroalimentacion);
public sealed record NotasCurso(Guid CursoId, IReadOnlyList<NotaActividad> Actividades);
public sealed record ArchivoEntrega(Guid Id, string NombreArchivo, long Tamano);
public sealed record EntregaPropia(Guid Id, Guid ActividadId, DateTime FechaEnvio, string Estado, IReadOnlyList<ArchivoEntrega> Archivos);
public sealed record CursoProfesor(Guid Id, string Codigo, string Nombre);
public sealed record EstudianteInscrito(Guid Id, string Nombre, string Codigo);
public sealed record ActividadCurso(Guid Id, string Titulo, int Corte, decimal Peso);
public sealed record CalificacionActividad(Guid EstudianteId, decimal Valor, string Estado, string? Version);
