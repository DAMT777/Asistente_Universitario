using Evaluaciones.Api.Seguridad;
using Evaluaciones.Application.CasosDeUso;
using Evaluaciones.Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Evaluaciones.Api.Controllers;

/// <summary>Notas del estudiante del token. Nunca recibe el id del estudiante por la ruta (RN-16).</summary>
[ApiController]
[Authorize(Policy = Politicas.Estudiante)]
[Route("mis-notas")]
public sealed class MisNotasController(ConsultarMisNotas consultar) : ControllerBase
{
    /// <summary>CU-16. Matriz con los cortes publicados y la definitiva parcial.</summary>
    [HttpGet]
    public async Task<ActionResult<MatrizNotasDto>> Matriz(CancellationToken ct) =>
        Ok(await consultar.MatrizAsync(User.ObtenerId(), ct));

    /// <summary>CU-15. Notas publicadas y retroalimentación de las actividades del curso.</summary>
    [HttpGet("cursos/{cursoId:guid}/actividades")]
    public async Task<ActionResult<IReadOnlyList<NotaActividadDto>>> Actividades(Guid cursoId, CancellationToken ct) =>
        Ok(await consultar.ActividadesAsync(User.ObtenerId(), cursoId, ct));
}
