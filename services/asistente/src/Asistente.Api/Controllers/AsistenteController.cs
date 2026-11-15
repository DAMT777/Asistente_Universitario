using System.Text.Json;
using Asistente.Application;
using Asistente.Application.CasosDeUso;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Unillanos.ServiceDefaults.Seguridad;

namespace Asistente.Api.Controllers;

/// <summary>Contrato: contracts/asistente.yaml. Solo traduce HTTP a casos de uso.</summary>
[ApiController]
[Authorize(Policy = Politicas.EstudianteOProfesor)]
[Route("asistente")]
public sealed class AsistenteController : ControllerBase
{
    [HttpPost("mensajes")]
    [EnableRateLimiting(LimiteMensajes.Politica)]
    public Task<RespuestaMensaje> Mensaje([FromBody] SolicitudMensaje solicitud, [FromServices] ProcesarMensaje casoDeUso, CancellationToken ct) =>
        casoDeUso.EjecutarAsync(UsuarioActual(), solicitud, ct);

    [HttpPost("acciones/{id:guid}/confirmar")]
    public async Task<JsonElement> Confirmar(Guid id, [FromServices] GestionarAcciones casoDeUso, CancellationToken ct) =>
        await casoDeUso.ConfirmarAsync(UsuarioActual(), id, ct);

    [HttpPost("acciones/{id:guid}/cancelar")]
    public async Task<IActionResult> Cancelar(Guid id, [FromServices] GestionarAcciones casoDeUso, CancellationToken ct)
    {
        await casoDeUso.CancelarAsync(UsuarioActual(), id, ct);
        return Ok(new { id, estado = "CANCELADA" });
    }

    /// <summary>sub y rol salen del token ya validado; nunca del cuerpo ni del modelo.</summary>
    private UsuarioAsistente UsuarioActual() =>
        new(User.ObtenerUsuarioId(), User.EsProfesor() ? UsuarioAsistente.Profesor : UsuarioAsistente.Estudiante);
}
