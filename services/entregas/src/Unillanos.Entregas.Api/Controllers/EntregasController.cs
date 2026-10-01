using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Unillanos.Entregas.Application.CasosDeUso;
using Unillanos.Entregas.Application.Comun;
using Unillanos.ServiceDefaults.Errores;
using Unillanos.ServiceDefaults.Seguridad;

namespace Unillanos.Entregas.Api.Controllers;

/// <summary>CU-13 y CU-14 del rol ESTUDIANTE. Solo traduce HTTP a casos de uso.</summary>
[ApiController]
[Authorize(Policy = Politicas.SoloEstudiante)]
[ProducesResponseType<ErrorApi>(StatusCodes.Status401Unauthorized, EscritorErrores.ContentType)]
[ProducesResponseType<ErrorApi>(StatusCodes.Status403Forbidden, EscritorErrores.ContentType)]
public sealed class EntregasController : ControllerBase
{
    [HttpPost("actividades/{actividadId}/entregas")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<EntregaRespuesta>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Subir(Guid actividadId, IFormFile? archivo, [FromServices] SubirEntrega casoDeUso, CancellationToken ct)
    {
        await using var contenido = archivo?.OpenReadStream();
        var comando = new SubirEntregaComando(actividadId, User.ObtenerUsuarioId(), ArchivoDe(archivo, contenido));
        var entrega = await casoDeUso.EjecutarAsync(comando, ct);
        return StatusCode(StatusCodes.Status201Created, entrega);
    }

    [HttpPut("entregas/{entregaId}")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<EntregaRespuesta>(StatusCodes.Status200OK)]
    public async Task<EntregaRespuesta> Editar(Guid entregaId, IFormFile? archivo, [FromServices] EditarEntrega casoDeUso, CancellationToken ct)
    {
        await using var contenido = archivo?.OpenReadStream();
        var comando = new EditarEntregaComando(entregaId, User.ObtenerUsuarioId(), ArchivoDe(archivo, contenido));
        return await casoDeUso.EjecutarAsync(comando, ct);
    }

    [HttpDelete("entregas/{entregaId}")]
    [ProducesResponseType<EntregaRespuesta>(StatusCodes.Status200OK)]
    public Task<EntregaRespuesta> Anular(Guid entregaId, [FromServices] AnularEntrega casoDeUso, CancellationToken ct)
        => casoDeUso.EjecutarAsync(entregaId, User.ObtenerUsuarioId(), ct);

    [HttpGet("mis-entregas")]
    [ProducesResponseType<IReadOnlyList<EntregaRespuesta>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<EntregaRespuesta>> MisEntregas([FromQuery] Guid? actividadId, [FromServices] ListarMisEntregas casoDeUso, CancellationToken ct)
        => casoDeUso.EjecutarAsync(User.ObtenerUsuarioId(), actividadId, ct);

    private static ArchivoEntrante? ArchivoDe(IFormFile? archivo, Stream? contenido)
        => archivo is null || contenido is null ? null : new ArchivoEntrante(archivo.FileName, archivo.Length, contenido);
}
