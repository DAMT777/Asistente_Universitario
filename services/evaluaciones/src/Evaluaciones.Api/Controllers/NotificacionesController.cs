using Evaluaciones.Api.Seguridad;
using Evaluaciones.Application.CasosDeUso;
using Evaluaciones.Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Evaluaciones.Api.Controllers;

/// <summary>Notificaciones del usuario del token (cualquier rol; hoy solo los estudiantes las reciben).</summary>
[ApiController]
[Authorize]
[Route("notificaciones")]
public sealed class NotificacionesController(GestionarNotificaciones gestionar) : ControllerBase
{
    /// <summary>Las 50 más recientes y el total sin leer.</summary>
    [HttpGet]
    public async Task<ActionResult<NotificacionesDto>> Listar(CancellationToken ct) =>
        Ok(await gestionar.ListarAsync(User.ObtenerId(), ct));

    /// <summary>Marca una notificación propia como leída. Marcarla de nuevo no cambia nada.</summary>
    [HttpPost("{notificacionId:guid}/leida")]
    public async Task<IActionResult> MarcarLeida(Guid notificacionId, CancellationToken ct)
    {
        await gestionar.MarcarLeidaAsync(User.ObtenerId(), notificacionId, ct);
        return NoContent();
    }

    /// <summary>Marca como leídas todas las notificaciones del usuario.</summary>
    [HttpPost("leidas")]
    public async Task<ActionResult<ResultadoMarcarLeidas>> MarcarTodasLeidas(CancellationToken ct) =>
        Ok(await gestionar.MarcarTodasLeidasAsync(User.ObtenerId(), ct));
}
