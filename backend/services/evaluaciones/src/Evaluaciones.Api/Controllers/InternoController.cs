using Evaluaciones.Api.Seguridad;
using Evaluaciones.Application.CasosDeUso;
using Evaluaciones.Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Evaluaciones.Api.Controllers;

/// <summary>
/// Endpoint interno de la sección 8.6. No pasa por el gateway (que bloquea /internal/**) y no usa el token
/// del usuario: lo llama el servicio de entregas con X-Service-Key.
/// </summary>
[ApiController]
[AllowAnonymous]
[ClaveDeServicio]
[Route("internal/actividades")]
public sealed class InternoController(ConsultarActividadInterna consultar) : ControllerBase
{
    [HttpGet("{actividadId:guid}")]
    public async Task<ActionResult<ActividadInternaDto>> Actividad(
        Guid actividadId, [FromQuery] Guid? estudianteId, CancellationToken ct) =>
        Ok(await consultar.EjecutarAsync(actividadId, estudianteId, ct));
}
