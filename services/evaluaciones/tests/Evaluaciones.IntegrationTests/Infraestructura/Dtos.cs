namespace Evaluaciones.IntegrationTests.Infraestructura;

public sealed record ErrorDto(int Status, string Codigo, string Mensaje, string TraceId);

public sealed record NotasActividadesDto(Guid CursoId, List<NotaActividadDto> Actividades);

public sealed record NotaActividadDto(Guid ActividadId, string Titulo, int Corte, decimal Peso, DateTimeOffset FechaLimite, string Estado, decimal? Nota, string? Retroalimentacion);

public sealed record MatrizDto(List<CursoMatrizDto> Cursos);

public sealed record CursoMatrizDto(Guid CursoId, string Codigo, string Nombre, string Profesor, List<CorteDto> Cortes, decimal DefinitivaParcial, bool EsParcial);

public sealed record CorteDto(int Corte, decimal PesoCorte, decimal? Nota, bool Publicado);

public sealed record ActividadInternaDto(Guid ActividadId, Guid CursoId, Guid ProfesorId, DateTimeOffset FechaLimite, bool RequiereEntrega, bool EstudianteInscrito);
