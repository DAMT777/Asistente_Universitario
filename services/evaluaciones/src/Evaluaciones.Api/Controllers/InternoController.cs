using Evaluaciones.Api.Seguridad;
using Evaluaciones.Application.CasosDeUso;
using Evaluaciones.Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Evaluaciones.Api.Controllers;

/// <summary>Servicio a servicio (Entregas). Sin JWT: exige X-Service-Key. El gateway bloquea /internal/**.</summary>
[ApiController]
[AllowAnonymous]
[ServiceFilter<ClaveServicioFilter>]
[Route("internal")]
public sealed class InternoController(ObtenerActividadInterna obtener) : ControllerBase
{
    /// <param name="estudianteId">Opcional. Con él se informa si está inscrito; Entregas lo omite cuando consulta un profesor.</param>
    [HttpGet("actividades/{actividadId:guid}")]
    public async Task<ActionResult<ActividadInternaDto>> Actividad(Guid actividadId, [FromQuery] Guid? estudianteId, CancellationToken ct) =>
        Ok(await obtener.EjecutarAsync(actividadId, estudianteId, ct));
}
