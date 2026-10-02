using Evaluaciones.Domain.Entidades;

namespace Evaluaciones.Application.Dtos;

/// <summary>Cuerpo de PUT /cursos/{cursoId}/pesos (CU-02). Los tres deben sumar 100 (RN-01).</summary>
public sealed record SolicitudPesosCortes(decimal? PesoCorte1, decimal? PesoCorte2, decimal? PesoCorte3);

/// <summary>Cuerpo de POST /cursos/{cursoId}/actividades y PUT /actividades/{actividadId} (CU-03).</summary>
/// <param name="Peso">Porcentaje dentro del corte, mayor que 0. Las actividades del corte no pueden pasar de 100 (RN-02).</param>
/// <param name="FechaLimite">Obligatoria si requiere entrega. Se guarda en UTC.</param>
public sealed record SolicitudActividad(
    string? Titulo,
    int? Corte,
    decimal? Peso,
    DateTimeOffset? FechaLimite,
    bool? RequiereEntrega);

/// <summary>CU-04 y CU-13. Lo que el servicio de entregas necesita saber de una actividad (sección 8.6).</summary>
public sealed record ActividadInternaDto(
    Guid ActividadId,
    Guid CursoId,
    Guid ProfesorId,
    DateTime? FechaLimite,
    bool RequiereEntrega,
    bool? EstudianteInscrito);

internal static class MapeoActividad
{
    public static ActividadDto ADto(this Actividad a, DateTime ahoraUtc, string? estado = null) =>
        new(
            a.Id,
            a.CursoId,
            a.Titulo,
            a.Corte,
            a.Peso,
            a.FechaLimite,
            a.RequiereEntrega,
            Vencida: a.FechaLimite is not null && ahoraUtc > a.FechaLimite,
            estado);

    public static CursoResumenDto AResumen(this Curso c, int? pendientes = null) =>
        new(c.Id, c.Codigo, c.Nombre, c.ProfesorNombre, c.PesoCorte1, c.PesoCorte2, c.PesoCorte3, pendientes);
}
