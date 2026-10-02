using Evaluaciones.Api.Seguridad;
using Evaluaciones.Application.CasosDeUso;
using Evaluaciones.Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Evaluaciones.Api.Controllers;

[ApiController]
[Authorize(Policy = Politicas.Profesor)]
[Route("actividades")]
public sealed class ActividadesController(ListarCalificacionesDeActividad listarCalificaciones) : ControllerBase
{
    /// <summary>Calificaciones de la actividad en cualquier estado. Apoyo de CU-09, CU-10 y CU-11.</summary>
    [HttpGet("{actividadId:guid}/calificaciones")]
    public async Task<ActionResult<IReadOnlyList<CalificacionDto>>> Calificaciones(Guid actividadId, CancellationToken ct) =>
        Ok(await listarCalificaciones.EjecutarAsync(User.ObtenerId(), actividadId, ct));
}
