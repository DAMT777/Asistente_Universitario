namespace Evaluaciones.Domain.Ponderado;

/// <summary>
/// DA-01 (propuesta de trabajo): se calcula con decimal sin redondear y solo se redondea al presentar,
/// a 1 decimal y alejándose de cero en el punto medio. Único lugar donde cambiar la regla.
/// </summary>
public static class PoliticaRedondeo
{
    public const int Decimales = 1;
    public const MidpointRounding Modo = MidpointRounding.AwayFromZero;

    // Sumar 0.0m fija la escala a un decimal (0 -> 0.0, 1 -> 1.0) sin cambiar el valor.
    public static decimal Presentar(decimal valor) => decimal.Round(valor, Decimales, Modo) + 0.0m;

    public static decimal? Presentar(decimal? valor) => valor is { } v ? Presentar(v) : null;
}
