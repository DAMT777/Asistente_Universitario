namespace Unillanos.Evaluaciones.Domain.Ponderado;

/// <summary>Calificación publicada de una actividad con el peso de la actividad dentro de su corte.</summary>
public readonly record struct NotaActividad(decimal Valor, decimal Peso);

/// <summary>Un corte del curso: su peso y la nota publicada (null si no se ha publicado).</summary>
public readonly record struct CorteCurso(int Corte, decimal PesoCorte, decimal? NotaPublicada)
{
    public bool Publicado => NotaPublicada.HasValue;
}

public readonly record struct Definitiva(decimal Valor, bool EsParcial);

/// <summary>
/// RN-10 / RN-11. Código puro: el ponderado se calcula siempre y nunca se guarda.
/// No redondea; eso le corresponde a <see cref="PoliticaRedondeo"/> al presentar.
/// </summary>
public static class CalculadoraPonderado
{
    /// <summary>Aporte de una actividad = valor x peso / 100.</summary>
    public static decimal Aporte(NotaActividad nota) => nota.Valor * nota.Peso / 100m;

    /// <summary>Nota de un corte = suma de los aportes de sus actividades calificadas y publicadas.</summary>
    public static decimal NotaCorte(IEnumerable<NotaActividad> publicadas) => publicadas.Sum(Aporte);

    /// <summary>
    /// Definitiva parcial = suma de (nota del corte x peso del corte / 100) solo sobre cortes publicados.
    /// Es parcial mientras algún corte no esté publicado; sin cortes publicados vale 0.
    /// </summary>
    public static Definitiva DefinitivaParcial(IReadOnlyCollection<CorteCurso> cortes)
    {
        var valor = cortes.Where(c => c.Publicado).Sum(c => c.NotaPublicada!.Value * c.PesoCorte / 100m);
        var esParcial = cortes.Count == 0 || cortes.Any(c => !c.Publicado);
        return new Definitiva(valor, esParcial);
    }
}
