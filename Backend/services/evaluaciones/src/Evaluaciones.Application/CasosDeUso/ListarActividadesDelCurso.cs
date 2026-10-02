using Evaluaciones.Application.Abstracciones;
using Evaluaciones.Application.Dtos;
using Evaluaciones.Application.Internos;
using Evaluaciones.Domain;
using Evaluaciones.Domain.Entidades;
using Evaluaciones.Domain.Errores;

namespace Evaluaciones.Application.CasosDeUso;

/// <summary>
/// CU-12 (parte 2). Actividades de un curso. El estudiante ve su estado en cada una y puede filtrar
/// solo las pendientes (el filtro se ignora para el profesor). Un estudiante solo accede a cursos
/// donde está inscrito (RN-16) y un profesor solo a los suyos (RN-15).
/// </summary>
public sealed class ListarActividadesDelCurso(
    ICursoRepository cursos,
    IInscripcionRepository inscripciones,
    IActividadRepository actividades,
    ICalificacionRepository calificaciones,
    TimeProvider reloj)
{
    public async Task<IReadOnlyList<ActividadDto>> EjecutarAsync(
        Guid usuarioId, string rol, Guid cursoId, bool soloPendientes, CancellationToken ct)
    {
        var ahora = reloj.GetUtcNow().UtcDateTime;

        if (rol == Roles.Profesor)
        {
            await AccesoCurso.ObtenerDelProfesorAsync(cursos, cursoId, usuarioId, ct);
            var todas = await actividades.ListarActividadesAsync(cursoId, ct);
            return Ordenar(todas).Select(a => Dto(a, ahora, null)).ToList();
        }

        if (rol == Roles.Estudiante)
        {
            if (!await inscripciones.EstaInscritoAsync(cursoId, usuarioId, ct))
                throw new DominioException(CodigosError.NoEncontrado, "El curso no existe o no estás inscrito.");

            var delCurso = await actividades.ListarActividadesAsync(cursoId, ct);
            var yaCalificadas = (await calificaciones.ListarCalificacionesPublicadasAsync(usuarioId, ct))
                .Select(c => c.ActividadId)
                .ToHashSet();

            var dtos = Ordenar(delCurso)
                .Select(a => Dto(a, ahora, yaCalificadas.Contains(a.Id) ? "CALIFICADA" : "PENDIENTE"));

            return (soloPendientes ? dtos.Where(d => d.Estado == "PENDIENTE") : dtos).ToList();
        }

        throw new DominioException(CodigosError.SinPermiso, "El rol no puede consultar actividades.");
    }

    private static IEnumerable<Actividad> Ordenar(IEnumerable<Actividad> actividades) =>
        actividades
            .OrderBy(a => a.Corte)
            .ThenBy(a => a.FechaLimite is null)
            .ThenBy(a => a.FechaLimite)
            .ThenBy(a => a.Titulo);

    private static ActividadDto Dto(Actividad actividad, DateTime ahoraUtc, string? estado) =>
        new(
            actividad.Id,
            actividad.CursoId,
            actividad.Titulo,
            actividad.Corte,
            actividad.Peso,
            actividad.FechaLimite,
            actividad.RequiereEntrega,
            Vencida: actividad.FechaLimite is not null && ahoraUtc > actividad.FechaLimite,
            estado);
}
