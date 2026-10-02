namespace Evaluaciones.Application.Dtos;

/// <param name="ActividadesPendientes">Solo para el estudiante: actividades del curso sin calificación publicada.</param>
public sealed record CursoResumenDto(
    Guid Id,
    string Codigo,
    string Nombre,
    string Profesor,
    decimal PesoCorte1,
    decimal PesoCorte2,
    decimal PesoCorte3,
    int? ActividadesPendientes);

/// <param name="Vencida">La fecha límite ya pasó. Una actividad sin fecha límite nunca está vencida.</param>
/// <param name="Estado">
/// Solo para el estudiante: PENDIENTE (sin calificación publicada) o CALIFICADA.
/// Si entregó o no lo sabe el servicio de entregas y lo combina el frontend.
/// </param>
public sealed record ActividadDto(
    Guid Id,
    Guid CursoId,
    string Titulo,
    int Corte,
    decimal Peso,
    DateTime? FechaLimite,
    bool RequiereEntrega,
    bool Vencida,
    string? Estado);

/// <summary>Estudiante inscrito. Misma forma que un usuario del servicio de usuarios, para que el cliente la reutilice.</summary>
public sealed record EstudianteInscritoDto(Guid Id, string Nombre, string Codigo, string Rol);

/// <param name="Estado">BORRADOR o PUBLICADA.</param>
/// <param name="Version">ROWVERSION en base64 (para If-Match).</param>
public sealed record CalificacionDto(
    Guid ActividadId,
    Guid EstudianteId,
    Guid? EntregaId,
    decimal Valor,
    string? Retroalimentacion,
    string Estado,
    string? Version);
