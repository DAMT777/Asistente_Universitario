using Evaluaciones.Application.Abstracciones;
using Evaluaciones.Application.Dtos;
using Evaluaciones.Domain.Entidades;
using Evaluaciones.Domain.Ponderado;

namespace Evaluaciones.Application.CasosDeUso;

/// <summary>
/// CU-16. Matriz de notas del estudiante: por curso, la nota publicada de cada corte (PublicacionCorte),
/// la definitiva parcial calculada con <see cref="MotorPonderado"/> sobre los cortes publicados y si aún es parcial.
/// Un corte sin publicar va con nota null y publicado = false (RN-14). La definitiva nunca se guarda.
/// </summary>
public sealed class ObtenerMatrizNotas(ICursoRepository cursos, IPublicacionCorteRepository publicaciones)
{
    public async Task<MatrizNotasDto> EjecutarAsync(Guid estudianteId, CancellationToken ct)
    {
        var inscritos = await cursos.ListarPorEstudianteAsync(estudianteId, ct);
        var propias = await publicaciones.ListarPublicacionesDelEstudianteAsync(estudianteId, ct);

        var filas = inscritos
            .Select(curso => Construir(curso, propias.Where(p => p.CursoId == curso.Id).ToList()))
            .ToList();
        return new MatrizNotasDto(filas);
    }

    private static CursoMatrizDto Construir(Curso curso, IReadOnlyList<PublicacionCorte> publicadas)
    {
        var notaPorCorte = publicadas.ToDictionary(p => p.Corte, p => p.Nota);
        var cortes = Enumerable.Range(1, MotorPonderado.CantidadCortes)
            .Select(k => new CorteMatrizDto(
                k,
                curso.PesoDeCorte(k),
                notaPorCorte.TryGetValue(k, out var nota) ? MotorPonderado.Redondear(nota) : null,
                notaPorCorte.ContainsKey(k)))
            .ToList();

        return new CursoMatrizDto(
            curso.Id,
            curso.Codigo,
            curso.Nombre,
            curso.ProfesorNombre,
            cortes,
            MotorPonderado.DefinitivaParcial(curso, notaPorCorte),
            EsParcial: cortes.Any(c => !c.Publicado));
    }
}
