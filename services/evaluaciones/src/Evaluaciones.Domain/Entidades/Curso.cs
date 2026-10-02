using Evaluaciones.Domain.Errores;

namespace Evaluaciones.Domain.Entidades;

public class Curso
{
    public Guid Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public Guid ProfesorId { get; set; }
    public string ProfesorNombre { get; set; } = "";
    public decimal PesoCorte1 { get; set; }
    public decimal PesoCorte2 { get; set; }
    public decimal PesoCorte3 { get; set; }

    public decimal PesoDeCorte(int corte) => corte switch
    {
        1 => PesoCorte1,
        2 => PesoCorte2,
        3 => PesoCorte3,
        _ => throw new ArgumentOutOfRangeException(nameof(corte), "El corte debe ser 1, 2 o 3.")
    };

    /// <summary>
    /// CU-02. Define el peso de los tres cortes (RN-01): cada uno entre 0 y 100, con dos decimales como máximo,
    /// y entre los tres exactamente 100.
    /// </summary>
    public void DefinirPesos(decimal? corte1, decimal? corte2, decimal? corte3)
    {
        if (corte1 is null || corte2 is null || corte3 is null)
            throw new DominioException(CodigosError.ValidacionFallida, "Indica el peso de los tres cortes.");

        decimal[] pesos = [corte1.Value, corte2.Value, corte3.Value];
        if (pesos.Any(p => p < 0 || p > 100 || decimal.Round(p, 2) != p))
            throw new DominioException(
                CodigosError.PesosCorteInvalidos,
                "Cada corte debe pesar entre 0 y 100, con dos decimales como máximo.");

        var suma = pesos.Sum();
        if (suma != 100)
            throw new DominioException(
                CodigosError.PesosCorteInvalidos,
                $"Los pesos de los tres cortes deben sumar 100. Ahora suman {suma:0.##}.");

        PesoCorte1 = pesos[0];
        PesoCorte2 = pesos[1];
        PesoCorte3 = pesos[2];
    }
}
