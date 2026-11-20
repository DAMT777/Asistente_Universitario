using Evaluaciones.Application.Abstracciones;
using Evaluaciones.Application.Dtos;
using Evaluaciones.Domain.Errores;

namespace Evaluaciones.Application.CasosDeUso;

/// <summary>
/// Notificaciones del usuario del token: las consulta y las marca como leídas.
/// Cada usuario solo ve y modifica las suyas; una ajena responde igual que una que no existe.
/// </summary>
public sealed class GestionarNotificaciones(INotificacionRepository notificaciones, IUnidadDeTrabajo unidadDeTrabajo)
{
    /// <summary>Cuántas devuelve como máximo la lista (el contador de no leídas sí las cuenta todas).</summary>
    public const int Maximo = 50;

    public async Task<NotificacionesDto> ListarAsync(Guid usuarioId, CancellationToken ct)
    {
        var lista = await notificaciones.ListarDelUsuarioAsync(usuarioId, Maximo, ct);
        var noLeidas = await notificaciones.ContarNoLeidasAsync(usuarioId, ct);
        return new NotificacionesDto(noLeidas, lista.Select(n => n.ADto()).ToList());
    }

    public async Task MarcarLeidaAsync(Guid usuarioId, Guid notificacionId, CancellationToken ct)
    {
        var notificacion = await notificaciones.ObtenerNotificacionAsync(notificacionId, ct);
        if (notificacion is null || notificacion.UsuarioId != usuarioId)
            throw new DominioException(CodigosError.NoEncontrado, "La notificación no existe.");

        if (notificacion.MarcarLeida())
            await unidadDeTrabajo.GuardarCambiosAsync(ct);
    }

    public async Task<ResultadoMarcarLeidas> MarcarTodasLeidasAsync(Guid usuarioId, CancellationToken ct)
    {
        var pendientes = await notificaciones.ListarNoLeidasAsync(usuarioId, ct);
        var marcadas = pendientes.Count(n => n.MarcarLeida());
        if (marcadas > 0)
            await unidadDeTrabajo.GuardarCambiosAsync(ct);
        return new ResultadoMarcarLeidas(marcadas);
    }
}
