using Evaluaciones.Api.Seguridad;
using Evaluaciones.Application.CasosDeUso;
using Evaluaciones.Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Evaluaciones.Api.Controllers;

/// <summary>CU-15 y CU-16 del rol estudiante. Solo recibe la petición y delega en un caso de uso.</summary>
[ApiController]
[Authorize(Policy = Politicas.Estudiante)]
[Route("mis-notas")]
public sealed class MisNotasController(ObtenerMatrizNotas matriz, ObtenerNotasActividades notasActividades) : ControllerBase
{
    /// <summary>CU-16. Matriz de notas con cortes publicados y definitiva parcial.</summary>
    [HttpGet]
    public async Task<ActionResult<MatrizNotasDto>> Matriz(CancellationToken ct) =>
        Ok(await matriz.EjecutarAsync(User.ObtenerId(), ct));

    /// <summary>CU-15. Nota publicada y retroalimentación de cada actividad del curso.</summary>
    [HttpGet("cursos/{cursoId:guid}/actividades")]
    public async Task<ActionResult<NotasActividadesDto>> Actividades(Guid cursoId, CancellationToken ct) =>
        Ok(await notasActividades.EjecutarAsync(User.ObtenerId(), cursoId, ct));
}
