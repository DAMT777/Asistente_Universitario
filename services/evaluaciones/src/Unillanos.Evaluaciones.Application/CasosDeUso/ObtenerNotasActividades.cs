using Unillanos.Evaluaciones.Application.Puertos;
using Unillanos.Evaluaciones.Domain;

namespace Unillanos.Evaluaciones.Application.CasosDeUso;

public sealed record NotasActividadesRespuesta(Guid CursoId, IReadOnlyList<NotaActividadRespuesta> Actividades);

public sealed record NotaActividadRespuesta(
    Guid ActividadId,
    string Titulo,
    int Corte,
    decimal Peso,
    DateTime FechaLimite,
    string Estado,
    decimal? Nota,
    string? Retroalimentacion);

public static class EstadosNotaEstudiante
{
    public const string Publicada = "PUBLICADA";
    public const string SinCalificar = "SIN_CALIFICAR";
}

/// <summary>
/// CU-15: nota y retroalimentación por actividad. Un borrador o una actividad sin calificar
/// se muestran igual: SIN_CALIFICAR con nota null (RN-08, RN-09). Sin calificar no es 0.
/// </summary>
public sealed class ObtenerNotasActividades(
    VerificadorInscripcion inscripcion,
    IActividadRepositorio actividades,
    ICalificacionRepositorio calificaciones)
{
    public async Task<NotasActividadesRespuesta> EjecutarAsync(Guid cursoId, Guid estudianteId, CancellationToken ct)
    {
        await inscripcion.ExigirAsync(cursoId, estudianteId, ct);

        var delCurso = await actividades.ListarDeCursoAsync(cursoId, ct);
        var publicadas = (await calificaciones.ListarPublicadasAsync(estudianteId, delCurso.Select(a => a.Id).ToList(), ct))
            .Where(c => c.EsVisibleParaEstudiante)
            .ToDictionary(c => c.ActividadId);

        var filas = delCurso
            .OrderBy(a => a.Corte).ThenBy(a => a.FechaLimite).ThenBy(a => a.Titulo, StringComparer.Ordinal)
            .Select(a => publicadas.TryGetValue(a.Id, out var c)
                ? new NotaActividadRespuesta(a.Id, a.Titulo, a.Corte, a.Peso, Utc(a.FechaLimite), EstadosNotaEstudiante.Publicada, c.Valor, c.Retroalimentacion)
                : new NotaActividadRespuesta(a.Id, a.Titulo, a.Corte, a.Peso, Utc(a.FechaLimite), EstadosNotaEstudiante.SinCalificar, null, null))
            .ToList();

        return new NotasActividadesRespuesta(cursoId, filas);
    }

    private static DateTime Utc(DateTime fecha) => DateTime.SpecifyKind(fecha, DateTimeKind.Utc);
}

/// <summary>RN-16: el curso debe existir (404) y el estudiante debe estar inscrito (403).</summary>
public sealed class VerificadorInscripcion(ICursoRepositorio cursos)
{
    public async Task ExigirAsync(Guid cursoId, Guid estudianteId, CancellationToken ct)
    {
        _ = await cursos.ObtenerAsync(cursoId, ct) ?? throw new NoEncontradoException("El curso");
        if (!await cursos.EstaInscritoAsync(cursoId, estudianteId, ct))
            throw new SinPermisoException("El estudiante no está inscrito en el curso.");
    }
}
