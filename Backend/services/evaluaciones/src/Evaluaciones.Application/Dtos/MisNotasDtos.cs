namespace Evaluaciones.Application.Dtos;

/// <summary>CU-16. Matriz de notas del estudiante (sección 8.7 de la guía).</summary>
public sealed record MatrizNotasDto(IReadOnlyList<MatrizCursoDto> Cursos);

/// <param name="DefinitivaParcial">Suma de nota × peso / 100 sobre los cortes publicados, a un decimal.</param>
/// <param name="EsParcial">Verdadero mientras falte algún corte por publicar (RN-14).</param>
public sealed record MatrizCursoDto(
    Guid CursoId,
    string Codigo,
    string Nombre,
    string Profesor,
    IReadOnlyList<MatrizCorteDto> Cortes,
    decimal DefinitivaParcial,
    bool EsParcial);

/// <param name="Nota">Nula si el corte no está publicado. Nunca 0 por falta de publicación (RN-14).</param>
public sealed record MatrizCorteDto(
    int Corte,
    decimal PesoCorte,
    decimal? Nota,
    bool Publicado,
    DateTime? FechaPublicacion);

/// <summary>CU-15. Nota publicada de una actividad con su retroalimentación. Solo PUBLICADA (RN-09).</summary>
public sealed record NotaActividadDto(
    Guid ActividadId,
    string Titulo,
    int Corte,
    decimal Peso,
    decimal Valor,
    string Retroalimentacion,
    string Estado);
