using Entregas.Domain;

namespace Entregas.UnitTests;

public sealed class LimitesEntregaTests
{
    private static readonly LimitesEntrega Limites = new(MaxBytesPorArchivo: 100, MaxBytesTotal: 250, MaxArchivos: 3);

    [Fact]
    public void Acepta_hasta_el_total_y_la_cantidad_maxima_exactos()
        => Limites.ValidarConjunto([100, 100, 50]);

    [Fact]
    public void Rechaza_si_la_suma_supera_el_total_con_413()
    {
        var ex = Assert.Throws<ArchivoDemasiadoGrandeException>(() => Limites.ValidarConjunto([100, 100, 51]));
        Assert.Equal("ARCHIVO_DEMASIADO_GRANDE", ex.Codigo);
    }

    [Fact]
    public void Rechaza_mas_archivos_que_el_maximo()
    {
        var ex = Assert.Throws<ArchivoInvalidoException>(() => Limites.ValidarConjunto([1, 1, 1, 1]));
        Assert.Equal("VALIDACION_FALLIDA", ex.Codigo);
    }

    [Fact]
    public void Rechaza_una_entrega_sin_archivos()
        => Assert.Throws<ArchivoInvalidoException>(() => Limites.ValidarConjunto([]));

    [Theory]
    [InlineData(20 * 1024 * 1024, "20 MB")]
    [InlineData(52428800, "50 MB")]
    [InlineData(1572864, "1.5 MB")]
    public void Muestra_los_limites_en_megas(long bytes, string esperado)
        => Assert.Equal(esperado, LimitesEntrega.Megas(bytes));
}
