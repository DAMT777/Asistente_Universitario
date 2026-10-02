using Entregas.Api.Seguridad;
using Entregas.Application.CasosDeUso;
using Entregas.Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Entregas.Api.Controllers;

/// <summary>Solo recibe la petición y delega en un caso de uso. La propiedad del recurso la verifica cada caso de uso.</summary>
[ApiController]
[Authorize]
public sealed class EntregasController(
    ListarEntregasDeActividad listarDeActividad,
    ListarMisEntregas listarMias,
    DescargarArchivo descargar) : ControllerBase
{
    /// <summary>CU-04. Entregas de una actividad, para el profesor dueño del curso.</summary>
    [HttpGet("actividades/{actividadId:guid}/entregas")]
    [Authorize(Policy = Politicas.Profesor)]
    public async Task<ActionResult<IReadOnlyList<EntregaDto>>> DeActividad(Guid actividadId, CancellationToken ct) =>
        Ok(await listarDeActividad.EjecutarAsync(User.ObtenerId(), actividadId, ct));

    /// <summary>Entregas propias del estudiante. Filtro opcional por actividad.</summary>
    [HttpGet("mis-entregas")]
    [Authorize(Policy = Politicas.Estudiante)]
    public async Task<ActionResult<IReadOnlyList<EntregaDto>>> Mias([FromQuery] Guid? actividadId, CancellationToken ct) =>
        Ok(await listarMias.EjecutarAsync(User.ObtenerId(), actividadId, ct));

    /// <summary>Descarga el archivo. Solo el profesor dueño del curso o el estudiante autor.</summary>
    [HttpGet("entregas/{entregaId:guid}/archivo")]
    public async Task<IActionResult> Archivo(Guid entregaId, CancellationToken ct)
    {
        var archivo = await descargar.EjecutarAsync(User.ObtenerId(), User.ObtenerRol(), entregaId, ct);
        return File(archivo.Contenido, archivo.TipoContenido, archivo.NombreArchivo);
    }
}
