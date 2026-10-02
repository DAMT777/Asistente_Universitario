using Evaluaciones.Api.Seguridad;
using Evaluaciones.Application.CasosDeUso;
using Evaluaciones.Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Evaluaciones.Api.Controllers;

/// <summary>Calificaciones de una actividad. Solo el profesor; la propiedad del curso la verifica cada caso de uso (RN-15).</summary>
[ApiController]
[Authorize(Policy = Politicas.Profesor)]
[Route("actividades")]
public sealed class ActividadesController(
    ListarCalificacionesDeActividad listarCalificaciones,
    CalificarActividad calificar,
    PublicarCalificacionesDeActividad publicarCalificaciones) : ControllerBase
{
    /// <summary>Calificaciones de la actividad en cualquier estado.</summary>
    [HttpGet("{actividadId:guid}/calificaciones")]
    public async Task<ActionResult<IReadOnlyList<CalificacionDto>>> Calificaciones(Guid actividadId, CancellationToken ct) =>
        Ok(await listarCalificaciones.EjecutarAsync(User.ObtenerId(), actividadId, ct));

    /// <summary>
    /// CU-05, CU-06 y CU-07. Crea o modifica la calificación de un estudiante. Queda en borrador.
    /// Acepta If-Match con la versión leída para detectar cambios simultáneos (409).
    /// </summary>
    [HttpPut("{actividadId:guid}/calificaciones/{estudianteId:guid}")]
    public async Task<ActionResult<CalificacionDto>> Calificar(
        Guid actividadId, Guid estudianteId, [FromBody] SolicitudCalificar solicitud, CancellationToken ct)
    {
        var resultado = await calificar.EjecutarAsync(
            User.ObtenerId(), actividadId, estudianteId, solicitud, Request.LeerVersionEsperada(), ct);

        Response.EscribirETag(resultado.Version);
        return Ok(resultado);
    }

    /// <summary>CU-08. Publica los borradores de la actividad para que los estudiantes los vean.</summary>
    [HttpPost("{actividadId:guid}/calificaciones/publicar")]
    public async Task<ActionResult<ResultadoPublicacionCalificaciones>> Publicar(Guid actividadId, CancellationToken ct) =>
        Ok(await publicarCalificaciones.EjecutarAsync(User.ObtenerId(), actividadId, ct));
}
