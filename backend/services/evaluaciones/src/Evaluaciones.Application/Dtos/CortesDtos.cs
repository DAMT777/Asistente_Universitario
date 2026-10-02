namespace Evaluaciones.Application.Dtos;

/// <param name="Estudiantes">Nulo publica para todos los inscritos.</param>
/// <param name="OmitirBorradores">Si es verdadero, los borradores se ignoran en lugar de rechazar la publicación (RN-12).</param>
public sealed record SolicitudPublicarCorte(
    IReadOnlyList<Guid>? Estudiantes = null,
    bool OmitirBorradores = false);

public sealed record SolicitudCorregirCorte(bool OmitirBorradores = false);

public sealed record ResultadoPublicacionCorte(
    int Corte,
    IReadOnlyList<CortePublicadoDto> Publicados,
    IReadOnlyList<CorteRechazadoDto> Rechazados);

public sealed record CortePublicadoDto(Guid EstudianteId, decimal Nota);

public sealed record CorteRechazadoDto(Guid EstudianteId, string Codigo, string Mensaje);

/// <param name="Version">ROWVERSION en base64, para usar como ETag e If-Match.</param>
public sealed record PublicacionCorteDto(
    Guid EstudianteId,
    int Corte,
    decimal Nota,
    DateTime FechaPublicacion,
    string? Version);
