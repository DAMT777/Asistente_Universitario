using Evaluaciones.Application.Abstracciones;
using Evaluaciones.Application.Dtos;
using Evaluaciones.Domain.Errores;

namespace Evaluaciones.Application.CasosDeUso;

/// <summary>
/// CU-15. Nota y retroalimentación del estudiante en cada actividad del curso. Solo se leen calificaciones
/// PUBLICADAS: un borrador o una actividad sin calificar salen como SIN_CALIFICAR con nota null (RN-08, RN-09).
/// El curso debe existir (404) y el estudiante debe estar inscrito (403, RN-16).
/// </summary>
public sealed class ObtenerNotasActividades(
    ICursoRepository cursos,
    IInscripcionRepository inscripciones,
    IActividadRepository actividades,
    ICalificacionRepository calificaciones)
{
    public const string Publicada = "PUBLICADA";
    public const string SinCalificar = "SIN_CALIFICAR";

    public async Task<NotasActividadesDto> EjecutarAsync(Guid estudianteId, Guid cursoId, CancellationToken ct)
    {
        _ = await cursos.ObtenerAsync(cursoId, ct)
            ?? throw new DominioException(CodigosError.NoEncontrado, "El curso no existe.");
        if (!await inscripciones.EstaInscritoAsync(cursoId, estudianteId, ct))
            throw new DominioException(CodigosError.SinPermiso, "No estás inscrito en este curso.");

        var delCurso = await actividades.ListarActividadesAsync(cursoId, ct);
        var publicadas = (await calificaciones.ListarCalificacionesPublicadasAsync(estudianteId, ct))
            .ToDictionary(c => c.ActividadId);

        var filas = delCurso
            .OrderBy(a => a.Corte).ThenBy(a => a.FechaLimite is null).ThenBy(a => a.FechaLimite).ThenBy(a => a.Titulo)
            .Select(a => publicadas.TryGetValue(a.Id, out var c)
                ? new NotaActividadDto(a.Id, a.Titulo, a.Corte, a.Peso, a.FechaLimite, Publicada, c.Valor,
                    string.IsNullOrWhiteSpace(c.Retroalimentacion) ? null : c.Retroalimentacion)
                : new NotaActividadDto(a.Id, a.Titulo, a.Corte, a.Peso, a.FechaLimite, SinCalificar, null, null))
            .ToList();

        return new NotasActividadesDto(cursoId, filas);
    }
}
