using Evaluaciones.Api.Seguridad;
using Evaluaciones.Application.CasosDeUso;
using Evaluaciones.Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Evaluaciones.Api.Controllers;

/// <summary>Solo recibe la petición y delega en un caso de uso (GRASP Controller).</summary>
[ApiController]
[Authorize]
[Route("cursos")]
public sealed class CursosController(
    ListarCursos listarCursos,
    ObtenerCurso obtenerCurso,
    DefinirPesosCortes definirPesos,
    GestionarActividad gestionarActividad,
    ListarActividadesDelCurso listarActividades,
    ListarEstudiantesDelCurso listarEstudiantes,
    ConsultarPonderado consultarPonderado,
    PublicarCorte publicarCorte,
    CorregirCorte corregirCorte) : ControllerBase
{
    /// <summary>Estudiantes inscritos en el curso. Apoyo de CU-09, CU-10 y CU-11.</summary>
    [HttpGet("{cursoId:guid}/estudiantes")]
    [Authorize(Policy = Politicas.Profesor)]
    public async Task<ActionResult<IReadOnlyList<EstudianteInscritoDto>>> ListarInscritos(Guid cursoId, CancellationToken ct) =>
        Ok(await listarEstudiantes.EjecutarAsync(User.ObtenerId(), cursoId, ct));

    /// <summary>CU-12. Cursos del profesor o del estudiante según el token.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CursoResumenDto>>> Listar(CancellationToken ct) =>
        Ok(await listarCursos.EjecutarAsync(User.ObtenerId(), User.ObtenerRol(), ct));

    /// <summary>Detalle del curso con los pesos de los cortes. Profesor dueño o estudiante inscrito.</summary>
    [HttpGet("{cursoId:guid}")]
    public async Task<ActionResult<CursoResumenDto>> Obtener(Guid cursoId, CancellationToken ct) =>
        Ok(await obtenerCurso.EjecutarAsync(User.ObtenerId(), User.ObtenerRol(), cursoId, ct));

    /// <summary>CU-02. Define los pesos de los tres cortes. Deben sumar 100.</summary>
    [HttpPut("{cursoId:guid}/pesos")]
    [Authorize(Policy = Politicas.Profesor)]
    public async Task<ActionResult<CursoResumenDto>> DefinirPesos(
        Guid cursoId, [FromBody] SolicitudPesosCortes solicitud, CancellationToken ct) =>
        Ok(await definirPesos.EjecutarAsync(User.ObtenerId(), cursoId, solicitud, ct));

    /// <summary>CU-03. Crea una actividad en el curso.</summary>
    [HttpPost("{cursoId:guid}/actividades")]
    [Authorize(Policy = Politicas.Profesor)]
    public async Task<ActionResult<ActividadDto>> CrearActividad(
        Guid cursoId, [FromBody] SolicitudActividad solicitud, CancellationToken ct)
    {
        var creada = await gestionarActividad.CrearAsync(User.ObtenerId(), cursoId, solicitud, ct);
        return Created($"/actividades/{creada.Id}", creada);
    }

    /// <summary>CU-12. Actividades del curso. El estudiante ve su estado en cada una.</summary>
    [HttpGet("{cursoId:guid}/actividades")]
    public async Task<ActionResult<IReadOnlyList<ActividadDto>>> ListarActividades(
        Guid cursoId, [FromQuery] bool soloPendientes, CancellationToken ct) =>
        Ok(await listarActividades.EjecutarAsync(User.ObtenerId(), User.ObtenerRol(), cursoId, soloPendientes, ct));

    /// <summary>CU-09. Ponderado calculado de todos los estudiantes del curso.</summary>
    [HttpGet("{cursoId:guid}/ponderado")]
    [Authorize(Policy = Politicas.Profesor)]
    public async Task<ActionResult<PonderadoCursoDto>> PonderadoDelCurso(Guid cursoId, CancellationToken ct) =>
        Ok(await consultarPonderado.PorCursoAsync(User.ObtenerId(), cursoId, ct));

    /// <summary>CU-09. Ponderado de un estudiante con el detalle por actividad.</summary>
    [HttpGet("{cursoId:guid}/ponderado/{estudianteId:guid}")]
    [Authorize(Policy = Politicas.Profesor)]
    public async Task<ActionResult<PonderadoEstudianteDto>> PonderadoDelEstudiante(
        Guid cursoId, Guid estudianteId, CancellationToken ct) =>
        Ok(await consultarPonderado.PorEstudianteAsync(User.ObtenerId(), cursoId, estudianteId, ct));

    /// <summary>CU-10. Publica el corte para uno o varios estudiantes.</summary>
    [HttpPost("{cursoId:guid}/cortes/{corte:int}/publicar")]
    [Authorize(Policy = Politicas.Profesor)]
    public async Task<ActionResult<ResultadoPublicacionCorte>> Publicar(
        Guid cursoId, int corte, [FromBody] SolicitudPublicarCorte? solicitud, CancellationToken ct) =>
        Ok(await publicarCorte.EjecutarAsync(
            User.ObtenerId(), cursoId, corte, solicitud ?? new SolicitudPublicarCorte(), ct));

    /// <summary>CU-11. Recalcula y corrige la publicación de un solo estudiante.</summary>
    [HttpPut("{cursoId:guid}/cortes/{corte:int}/estudiantes/{estudianteId:guid}")]
    [Authorize(Policy = Politicas.Profesor)]
    public async Task<ActionResult<PublicacionCorteDto>> Corregir(
        Guid cursoId, int corte, Guid estudianteId, [FromBody] SolicitudCorregirCorte? solicitud, CancellationToken ct)
    {
        var resultado = await corregirCorte.EjecutarAsync(
            User.ObtenerId(), cursoId, corte, estudianteId,
            solicitud ?? new SolicitudCorregirCorte(), Request.LeerVersionEsperada(), ct);

        Response.EscribirETag(resultado.Version);
        return Ok(resultado);
    }
}
