using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entregas.Application.CasosDeUso;
using Entregas.Application.Comun;
using Unillanos.ServiceDefaults.Errores;
using Unillanos.ServiceDefaults.Seguridad;

namespace Entregas.Api.Controllers;

/// <summary>
/// CU-13 y CU-14 del rol ESTUDIANTE, más las lecturas que necesita la pantalla de calificar del profesor.
/// Solo traduce HTTP a casos de uso.
/// </summary>
[ApiController]
[Authorize]
[ProducesResponseType<ErrorApi>(StatusCodes.Status401Unauthorized, EscritorErrores.ContentType)]
[ProducesResponseType<ErrorApi>(StatusCodes.Status403Forbidden, EscritorErrores.ContentType)]
public sealed class EntregasController : ControllerBase
{
    /// <summary>Campo multipart: se repite una vez por archivo.</summary>
    private const string CampoArchivo = "archivo";

    [HttpPost("actividades/{actividadId}/entregas")]
    [Authorize(Policy = Politicas.SoloEstudiante)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<EntregaRespuesta>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Subir(
        Guid actividadId, [FromForm(Name = CampoArchivo)] List<IFormFile>? archivos, [FromServices] SubirEntrega casoDeUso, CancellationToken ct)
    {
        await using var entrantes = new ArchivosEntrantes(archivos);
        var entrega = await casoDeUso.EjecutarAsync(new SubirEntregaComando(actividadId, User.ObtenerUsuarioId(), entrantes.Lista), ct);
        return StatusCode(StatusCodes.Status201Created, entrega);
    }

    [HttpPut("entregas/{entregaId}")]
    [Authorize(Policy = Politicas.SoloEstudiante)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<EntregaRespuesta>(StatusCodes.Status200OK)]
    public async Task<EntregaRespuesta> Editar(
        Guid entregaId, [FromForm(Name = CampoArchivo)] List<IFormFile>? archivos, [FromServices] EditarEntrega casoDeUso, CancellationToken ct)
    {
        await using var entrantes = new ArchivosEntrantes(archivos);
        return await casoDeUso.EjecutarAsync(new EditarEntregaComando(entregaId, User.ObtenerUsuarioId(), entrantes.Lista), ct);
    }

    [HttpDelete("entregas/{entregaId}")]
    [Authorize(Policy = Politicas.SoloEstudiante)]
    [ProducesResponseType<EntregaRespuesta>(StatusCodes.Status200OK)]
    public Task<EntregaRespuesta> Anular(Guid entregaId, [FromServices] AnularEntrega casoDeUso, CancellationToken ct)
        => casoDeUso.EjecutarAsync(entregaId, User.ObtenerUsuarioId(), ct);

    [HttpGet("mis-entregas")]
    [Authorize(Policy = Politicas.SoloEstudiante)]
    [ProducesResponseType<IReadOnlyList<EntregaRespuesta>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<EntregaRespuesta>> MisEntregas([FromQuery] Guid? actividadId, [FromServices] ListarMisEntregas casoDeUso, CancellationToken ct)
        => casoDeUso.EjecutarAsync(User.ObtenerUsuarioId(), actividadId, ct);

    /// <summary>Entregas de todos los estudiantes en la actividad. Solo el profesor dueño del curso.</summary>
    [HttpGet("actividades/{actividadId}/entregas")]
    [Authorize(Policy = Politicas.SoloProfesor)]
    [ProducesResponseType<IReadOnlyList<EntregaRespuesta>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorApi>(StatusCodes.Status404NotFound, EscritorErrores.ContentType)]
    public Task<IReadOnlyList<EntregaRespuesta>> DeActividad(Guid actividadId, [FromServices] ListarEntregasDeActividad casoDeUso, CancellationToken ct)
        => casoDeUso.EjecutarAsync(actividadId, User.ObtenerUsuarioId(), ct);

    /// <summary>
    /// Descarga un archivo con su nombre original (Content-Disposition: attachment).
    /// El estudiante dueño de la entrega o el profesor dueño del curso.
    /// </summary>
    [HttpGet("entregas/{entregaId}/archivos/{archivoId}")]
    [Authorize(Policy = Politicas.EstudianteOProfesor)]
    [ProducesResponseType<FileStreamResult>(StatusCodes.Status200OK, "application/octet-stream")]
    [ProducesResponseType<ErrorApi>(StatusCodes.Status404NotFound, EscritorErrores.ContentType)]
    public async Task<FileStreamResult> Descargar(Guid entregaId, Guid archivoId, [FromServices] DescargarArchivo casoDeUso, CancellationToken ct)
    {
        var archivo = await casoDeUso.EjecutarAsync(entregaId, archivoId, User.ObtenerUsuarioId(), User.EsProfesor(), ct);
        return File(archivo.Contenido, archivo.ContentType, archivo.NombreArchivo);
    }

    /// <summary>Abre los flujos de los archivos del formulario y los cierra al terminar la solicitud.</summary>
    private sealed class ArchivosEntrantes(List<IFormFile>? archivos) : IAsyncDisposable
    {
        public IReadOnlyList<ArchivoEntrante> Lista { get; } =
            (archivos ?? []).Select(a => new ArchivoEntrante(a.FileName, a.Length, a.OpenReadStream())).ToList();

        public async ValueTask DisposeAsync()
        {
            foreach (var a in Lista) await a.Contenido.DisposeAsync();
        }
    }
}
