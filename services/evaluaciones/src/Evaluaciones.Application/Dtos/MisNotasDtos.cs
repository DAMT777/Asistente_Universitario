namespace Evaluaciones.Application.Dtos;

// Formas de contracts/evaluaciones.yaml para el rol estudiante (CU-15 y CU-16).

/// <param name="Nota">Nota publicada del corte; null si no se ha publicado (nunca 0).</param>
public sealed record CorteMatrizDto(int Corte, decimal PesoCorte, decimal? Nota, bool Publicado);

/// <param name="DefinitivaParcial">Suma de nota × peso / 100 solo sobre cortes publicados; 0.0 si no hay ninguno.</param>
public sealed record CursoMatrizDto(
    Guid CursoId,
    string Codigo,
    string Nombre,
    string Profesor,
    IReadOnlyList<CorteMatrizDto> Cortes,
    decimal DefinitivaParcial,
    bool EsParcial);

public sealed record MatrizNotasDto(IReadOnlyList<CursoMatrizDto> Cursos);

/// <param name="Estado">PUBLICADA o SIN_CALIFICAR. Un borrador llega como SIN_CALIFICAR: el estudiante nunca lo ve.</param>
/// <param name="FechaLimite">UTC; null si la actividad no tiene fecha límite.</param>
public sealed record NotaActividadDto(
    Guid ActividadId,
    string Titulo,
    int Corte,
    decimal Peso,
    DateTime? FechaLimite,
    string Estado,
    decimal? Nota,
    string? Retroalimentacion);

public sealed record NotasActividadesDto(Guid CursoId, IReadOnlyList<NotaActividadDto> Actividades);

/// <summary>Respuesta de GET /internal/actividades/{id} para el servicio de entregas.</summary>
/// <param name="FechaLimite">UTC; null si la actividad no tiene fecha límite.</param>
/// <param name="EstudianteInscrito">False si no se indicó estudiante.</param>
public sealed record ActividadInternaDto(
    Guid ActividadId,
    Guid CursoId,
    Guid ProfesorId,
    DateTime? FechaLimite,
    bool RequiereEntrega,
    bool EstudianteInscrito);
