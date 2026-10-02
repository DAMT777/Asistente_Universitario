using Evaluaciones.Api.Autenticacion;
using Evaluaciones.Api.Contratos;
using Evaluaciones.Application.Calificaciones;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Evaluaciones.Api.Controllers;

/// <summary>
/// Casos de uso del profesor sobre calificaciones. Solo recibe la petición y delega (GRASP Controller):
/// la lógica vive en Domain y Application, y los errores los traduce un único middleware.
/// </summary>
[ApiController]
[Authorize(Policy = Politicas.Profesor)]
public sealed class CalificacionesController(
    GuardarCalificacion guardar,
    PublicarCalificaciones publicar,
    ListarCalificaciones listar) : ControllerBase
{
    /// <summary>Calificaciones existentes de la actividad, con su versión. Quien no aparece está sin calificar.</summary>
    [HttpGet("actividades/{actividadId}/calificaciones")]
    public async Task<ActionResult<IReadOnlyList<CalificacionDto>>> Listar(Guid actividadId, CancellationToken ct) =>
        Ok(await listar.EjecutarAsync(User.ComoActor(), actividadId, ct));

    /// <summary>
    /// CU-05 (calificar una entrega), CU-06 (nota de una actividad sin entrega) y CU-07 (modificar una nota).
    /// Crea la calificación o modifica la existente; en ambos casos queda en BORRADOR hasta publicar la actividad.
    /// El encabezado If-Match (opcional) lleva la versión leída para detectar modificaciones simultáneas.
    /// </summary>
    [HttpPut("actividades/{actividadId}/calificaciones/{estudianteId}")]
    public async Task<ActionResult<CalificacionDto>> Guardar(
        Guid actividadId, Guid estudianteId, [FromBody] GuardarCalificacionSolicitud solicitud, CancellationToken ct)
    {
        var comando = new GuardarCalificacionComando(
            User.ComoActor(), actividadId, estudianteId,
            solicitud.Valor, solicitud.Retroalimentacion, solicitud.EntregaId,
            VersionDeIfMatch(Request.Headers.IfMatch.FirstOrDefault()));

        var dto = await guardar.EjecutarAsync(comando, ct);
        Response.Headers.ETag = $"\"{dto.Version}\"";
        return Ok(dto);
    }

    /// <summary>
    /// CU-08. Publica las calificaciones en borrador de la actividad para que el estudiante las vea.
    /// El cuerpo es opcional: con estudianteIds publica solo las de esos estudiantes.
    /// </summary>
    [HttpPost("actividades/{actividadId}/calificaciones/publicar")]
    public async Task<ActionResult<ResultadoPublicacion>> Publicar(
        Guid actividadId,
        [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] PublicarCalificacionesSolicitud? solicitud,
        CancellationToken ct) =>
        Ok(await publicar.EjecutarAsync(new PublicarCalificacionesComando(User.ComoActor(), actividadId, solicitud?.EstudianteIds), ct));

    /// <summary>Acepta "AAAA=", "\"AAAA=\"" y W/"AAAA="; "*" significa cualquier versión.</summary>
    internal static string? VersionDeIfMatch(string? encabezado)
    {
        var valor = encabezado?.Trim();
        if (string.IsNullOrEmpty(valor) || valor == "*") return null;
        if (valor.StartsWith("W/", StringComparison.OrdinalIgnoreCase)) valor = valor[2..];
        return valor.Trim('"');
    }
}
