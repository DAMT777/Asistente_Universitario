using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Unillanos.Evaluaciones.Api.Seguridad;
using Unillanos.Evaluaciones.Application.CasosDeUso;
using Unillanos.Evaluaciones.Domain;
using Unillanos.ServiceDefaults.Errores;

namespace Unillanos.Evaluaciones.Api.Controllers;

/// <summary>
/// Endpoints servicio a servicio. No usan JWT sino X-Service-Key y el gateway debe bloquear /internal/**.
/// </summary>
[ApiController]
[Route("internal")]
[AllowAnonymous]
[ServiceFilter<RequiereClaveServicioFilter>]
public sealed class InternoController : ControllerBase
{
    [HttpGet("actividades/{actividadId}")]
    [ProducesResponseType<ActividadInternaRespuesta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorApi>(StatusCodes.Status404NotFound, EscritorErrores.ContentType)]
    public async Task<ActividadInternaRespuesta> Actividad(
        Guid actividadId, [FromQuery, BindRequired] Guid estudianteId, [FromServices] ObtenerActividadInterna casoDeUso, CancellationToken ct)
        => await casoDeUso.EjecutarAsync(actividadId, estudianteId, ct) ?? throw new NoEncontradoException("La actividad");
}
