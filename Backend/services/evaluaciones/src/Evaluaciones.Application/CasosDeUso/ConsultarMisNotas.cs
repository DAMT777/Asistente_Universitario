using Evaluaciones.Application.Abstracciones;
using Evaluaciones.Application.Dtos;
using Evaluaciones.Domain.Entidades;
using Evaluaciones.Domain.Errores;
using Evaluaciones.Domain.Ponderado;

namespace Evaluaciones.Application.CasosDeUso;

/// <summary>
/// CU-15 y CU-16. Lo que el estudiante ve de sus notas. Solo lo publicado (RN-09, RN-14) y solo lo suyo,
/// en cursos donde está inscrito (RN-16). Los borradores nunca salen de aquí.
/// </summary>
public sealed class ConsultarMisNotas(
    ICursoRepository cursos,
    IInscripcionRepository inscripciones,
    IActividadRepository actividades,
    ICalificacionRepository calificaciones,
    IPublicacionCorteRepository publicaciones)
{
    /// <summary>CU-16. Matriz de cortes publicados y definitiva parcial por curso.</summary>
    public async Task<MatrizNotasDto> MatrizAsync(Guid estudianteId, CancellationToken ct)
    {
        var susCursos = await cursos.ListarPorEstudianteAsync(estudianteId, ct);
        var publicadas = (await publicaciones.ListarPublicacionesDelEstudianteAsync(estudianteId, ct))
            .ToLookup(p => p.CursoId);

        var matriz = susCursos.Select(curso =>
        {
            var porCorte = publicadas[curso.Id].ToDictionary(p => p.Corte);
            var cortes = Enumerable.Range(1, MotorPonderado.CantidadCortes)
                .Select(k => porCorte.TryGetValue(k, out var p)
                    ? new MatrizCorteDto(k, curso.PesoDeCorte(k), p.Nota, true, p.FechaPublicacion)
                    : new MatrizCorteDto(k, curso.PesoDeCorte(k), null, false, null))
                .ToList();

            var notaPorCorte = porCorte.ToDictionary(x => x.Key, x => x.Value.Nota);
            return new MatrizCursoDto(
                curso.Id,
                curso.Codigo,
                curso.Nombre,
                curso.ProfesorNombre,
                cortes,
                MotorPonderado.DefinitivaParcial(curso, notaPorCorte),
                EsParcial: notaPorCorte.Count < MotorPonderado.CantidadCortes);
        }).ToList();

        return new MatrizNotasDto(matriz);
    }

    /// <summary>CU-15. Notas publicadas y retroalimentación de las actividades de un curso.</summary>
    public async Task<IReadOnlyList<NotaActividadDto>> ActividadesAsync(Guid estudianteId, Guid cursoId, CancellationToken ct)
    {
        if (!await inscripciones.EstaInscritoAsync(cursoId, estudianteId, ct))
            throw new DominioException(CodigosError.NoEncontrado, "El curso no existe o no estás inscrito.");

        var delCurso = (await actividades.ListarActividadesAsync(cursoId, ct)).ToDictionary(a => a.Id);
        var suyas = await calificaciones.ListarCalificacionesPublicadasAsync(estudianteId, ct);

        return suyas
            .Where(c => delCurso.ContainsKey(c.ActividadId))
            .Select(c => (Calificacion: c, Actividad: delCurso[c.ActividadId]))
            .OrderBy(x => x.Actividad.Corte)
            .ThenBy(x => x.Actividad.Titulo)
            .Select(x => new NotaActividadDto(
                x.Actividad.Id,
                x.Actividad.Titulo,
                x.Actividad.Corte,
                x.Actividad.Peso,
                x.Calificacion.Valor,
                x.Calificacion.Retroalimentacion ?? "",
                x.Calificacion.Estado.ComoTexto()))
            .ToList();
    }
}
