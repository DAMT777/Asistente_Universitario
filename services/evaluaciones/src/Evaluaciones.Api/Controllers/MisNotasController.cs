using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Evaluaciones.Application.CasosDeUso;
using Unillanos.ServiceDefaults.Errores;
using Unillanos.ServiceDefaults.Seguridad;

namespace Evaluaciones.Api.Controllers;

/// <summary>CU-15 y CU-16 del rol ESTUDIANTE.</summary>
[ApiController]
[Route("mis-notas")]
[Authorize(Policy = Politicas.SoloEstudiante)]
[ProducesResponseType<ErrorApi>(StatusCodes.Status401Unauthorized, EscritorErrores.ContentType)]
[ProducesResponseType<ErrorApi>(StatusCodes.Status403Forbidden, EscritorErrores.ContentType)]
public sealed class MisNotasController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<MatrizNotasRespuesta>(StatusCodes.Status200OK)]
    public Task<MatrizNotasRespuesta> Matriz([FromServices] ObtenerMatrizNotas casoDeUso, CancellationToken ct)
        => casoDeUso.EjecutarAsync(User.ObtenerUsuarioId(), ct);

    [HttpGet("cursos/{cursoId}/actividades")]
    [ProducesResponseType<NotasActividadesRespuesta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorApi>(StatusCodes.Status404NotFound, EscritorErrores.ContentType)]
    public Task<NotasActividadesRespuesta> Actividades(Guid cursoId, [FromServices] ObtenerNotasActividades casoDeUso, CancellationToken ct)
        => casoDeUso.EjecutarAsync(cursoId, User.ObtenerUsuarioId(), ct);
}
