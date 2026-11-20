using Evaluaciones.Domain.Entidades;

namespace Evaluaciones.Application.Dtos;

/// <param name="Tipo">NUEVA_ACTIVIDAD, CURSO_ACTUALIZADO o NOTA_PUBLICADA.</param>
/// <param name="ActividadId">Actividad relacionada, si la hay.</param>
public sealed record NotificacionDto(
    Guid Id,
    string Tipo,
    string Titulo,
    string Mensaje,
    Guid CursoId,
    Guid? ActividadId,
    DateTime FechaCreacion,
    bool Leida);

/// <param name="NoLeidas">Total sin leer, aunque no todas vengan en la lista.</param>
/// <param name="Notificaciones">Las más recientes primero.</param>
public sealed record NotificacionesDto(int NoLeidas, IReadOnlyList<NotificacionDto> Notificaciones);

/// <param name="Marcadas">Cuántas estaban sin leer.</param>
public sealed record ResultadoMarcarLeidas(int Marcadas);

internal static class MapeoNotificacion
{
    public static NotificacionDto ADto(this Notificacion n) =>
        new(n.Id, n.Tipo.ComoTexto(), n.Titulo, n.Mensaje, n.CursoId, n.ActividadId,
            DateTime.SpecifyKind(n.FechaCreacion, DateTimeKind.Utc), n.Leida);
}
