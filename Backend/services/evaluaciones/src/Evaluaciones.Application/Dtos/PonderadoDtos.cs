using System.Text.Json.Serialization;

namespace Evaluaciones.Application.Dtos;

/// <param name="Publicaciones">Cortes ya publicados, por estudiante.</param>
public sealed record PonderadoCursoDto(
    Guid CursoId,
    string Curso,
    IReadOnlyList<PonderadoEstudianteDto> Estudiantes,
    IReadOnlyList<PublicacionResumenDto> Publicaciones);

public sealed record PublicacionResumenDto(Guid EstudianteId, int Corte, decimal Nota, DateTime FechaPublicacion);

/// <param name="DefinitivaParcial">Suma de los cortes con nota calculada, redondeada a un decimal.</param>
/// <param name="EsParcial">Verdadero mientras algún corte no tenga ninguna actividad calificada.</param>
/// <param name="IncluyeBorradores">Verdadero si el cálculo usa calificaciones en borrador (solo lo ve el profesor).</param>
public sealed record PonderadoEstudianteDto(
    Guid EstudianteId,
    string Nombre,
    string Codigo,
    IReadOnlyList<PonderadoCorteDto> Cortes,
    decimal DefinitivaParcial,
    bool EsParcial,
    bool IncluyeBorradores);

/// <param name="NotaCorte">Calculada al consultar. Nula si el corte no tiene actividades calificadas (nunca 0).</param>
/// <param name="Publicado">Si existe la PublicacionCorte del estudiante en este corte.</param>
/// <param name="NotaPublicada">Nota guardada en la publicación, para ver si quedó desactualizada frente a NotaCorte.</param>
/// <param name="Actividades">Solo en el detalle de un estudiante.</param>
public sealed record PonderadoCorteDto(
    int Corte,
    decimal PesoCorte,
    decimal? NotaCorte,
    bool Publicado,
    decimal? NotaPublicada,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyList<PonderadoActividadDto>? Actividades);

/// <param name="Valor">Nulo si la actividad está sin calificar. Distinto de 0.0 (RN-08).</param>
/// <param name="Estado">BORRADOR, PUBLICADA, o nulo si está sin calificar.</param>
/// <param name="Aporte">valor × peso / 100.</param>
public sealed record PonderadoActividadDto(
    Guid ActividadId,
    string Titulo,
    decimal Peso,
    decimal? Valor,
    string? Estado,
    decimal? Aporte);
