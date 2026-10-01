using Evaluaciones.Application.Puertos;
using Evaluaciones.Domain.Entidades;
using Evaluaciones.Domain.Ponderado;

namespace Evaluaciones.Application.CasosDeUso;

public sealed record MatrizNotasRespuesta(IReadOnlyList<CursoMatrizRespuesta> Cursos);

public sealed record CursoMatrizRespuesta(
    Guid CursoId,
    string Codigo,
    string Nombre,
    string Profesor,
    IReadOnlyList<CorteMatrizRespuesta> Cortes,
    decimal DefinitivaParcial,
    bool EsParcial);

public sealed record CorteMatrizRespuesta(int Corte, decimal PesoCorte, decimal? Nota, bool Publicado);

/// <summary>
/// CU-16: matriz de notas por curso. La nota de cada corte sale de PublicacionCorte; un corte sin publicar
/// va con nota null y publicado=false (RN-14). La definitiva parcial se calcula, nunca se lee ni se guarda.
/// </summary>
public sealed class ObtenerMatrizNotas(ICursoRepositorio cursos, IPublicacionCorteRepositorio publicaciones)
{
    public async Task<MatrizNotasRespuesta> EjecutarAsync(Guid estudianteId, CancellationToken ct)
    {
        var inscritos = await cursos.ListarDeEstudianteAsync(estudianteId, ct);
        var vigentes = await publicaciones.ListarVigentesAsync(estudianteId, inscritos.Select(c => c.Id).ToList(), ct);

        var filas = inscritos
            .OrderBy(c => c.Codigo, StringComparer.Ordinal)
            .Select(c => Construir(c, vigentes.Where(p => p.CursoId == c.Id).ToList()))
            .ToList();
        return new MatrizNotasRespuesta(filas);
    }

    private static CursoMatrizRespuesta Construir(Curso curso, IReadOnlyList<PublicacionCorte> publicadas)
    {
        var cortes = Enumerable.Range(1, Curso.NumeroCortes)
            .Select(n => new CorteCurso(n, curso.PesoDe(n), publicadas.FirstOrDefault(p => p.Corte == n)?.Nota))
            .ToList();
        var definitiva = CalculadoraPonderado.DefinitivaParcial(cortes);

        return new CursoMatrizRespuesta(
            curso.Id,
            curso.Codigo,
            curso.Nombre,
            curso.ProfesorNombre,
            cortes.Select(c => new CorteMatrizRespuesta(c.Corte, c.PesoCorte, PoliticaRedondeo.Presentar(c.NotaPublicada), c.Publicado)).ToList(),
            PoliticaRedondeo.Presentar(definitiva.Valor),
            definitiva.EsParcial);
    }
}
