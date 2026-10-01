using Evaluaciones.Domain.Ponderado;

namespace Evaluaciones.UnitTests;

public sealed class CalculadoraPonderadoTests
{
    [Theory]
    [InlineData(4.0, 20, 0.8)]
    [InlineData(3.0, 80, 2.4)]
    [InlineData(5.0, 100, 5.0)]
    [InlineData(0.0, 50, 0.0)]
    public void Aporte_es_valor_por_peso_sobre_100(decimal valor, decimal peso, decimal esperado)
        => Assert.Equal(esperado, CalculadoraPonderado.Aporte(new NotaActividad(valor, peso)));

    [Fact]
    public void Nota_de_corte_de_Ana_es_3_2()
    {
        var nota = CalculadoraPonderado.NotaCorte([new(4.0m, 20), new(3.0m, 80)]);

        Assert.Equal(3.2m, nota);
    }

    [Fact]
    public void Nota_de_corte_sin_actividades_publicadas_es_0()
        => Assert.Equal(0m, CalculadoraPonderado.NotaCorte([]));

    [Fact]
    public void Definitiva_parcial_de_Ana_es_0_96_y_se_presenta_como_1_0()
    {
        var definitiva = CalculadoraPonderado.DefinitivaParcial([new(1, 30, 3.2m), new(2, 30, null), new(3, 40, null)]);

        Assert.Equal(0.96m, definitiva.Valor);
        Assert.True(definitiva.EsParcial);
        Assert.Equal(1.0m, PoliticaRedondeo.Presentar(definitiva.Valor));
    }

    [Fact]
    public void Cortes_sin_publicar_no_cuentan_como_cero_sino_que_se_excluyen()
    {
        CorteCurso sinPublicar = new(2, 30, null);

        Assert.False(sinPublicar.Publicado);
        Assert.Null(sinPublicar.NotaPublicada);
        Assert.Equal(1.5m, CalculadoraPonderado.DefinitivaParcial([new(1, 30, 5.0m), sinPublicar, new(3, 40, null)]).Valor);
    }

    [Fact]
    public void Sin_cortes_publicados_la_definitiva_es_0_0_y_parcial()
    {
        var definitiva = CalculadoraPonderado.DefinitivaParcial([new(1, 30, null), new(2, 30, null), new(3, 40, null)]);

        Assert.Equal(0m, definitiva.Valor);
        Assert.True(definitiva.EsParcial);
        Assert.Equal("0.0", PoliticaRedondeo.Presentar(definitiva.Valor).ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Con_todos_los_cortes_publicados_la_definitiva_ya_no_es_parcial()
    {
        var definitiva = CalculadoraPonderado.DefinitivaParcial([new(1, 30, 3.2m), new(2, 30, 4.0m), new(3, 40, 3.5m)]);

        Assert.Equal(3.56m, definitiva.Valor);
        Assert.False(definitiva.EsParcial);
    }

    [Fact]
    public void Sin_cortes_definidos_es_0_y_parcial()
    {
        var definitiva = CalculadoraPonderado.DefinitivaParcial([]);

        Assert.Equal(0m, definitiva.Valor);
        Assert.True(definitiva.EsParcial);
    }
}

public sealed class PoliticaRedondeoTests
{
    [Theory]
    [InlineData(0.96, 1.0)]
    [InlineData(0.25, 0.3)]
    [InlineData(2.45, 2.5)]
    [InlineData(2.44, 2.4)]
    [InlineData(3.2, 3.2)]
    [InlineData(4.95, 5.0)]
    public void Redondea_a_un_decimal_alejandose_de_cero(decimal valor, decimal esperado)
        => Assert.Equal(esperado, PoliticaRedondeo.Presentar(valor));

    [Fact]
    public void Null_se_mantiene_null()
        => Assert.Null(PoliticaRedondeo.Presentar((decimal?)null));

    [Fact]
    public void Siempre_presenta_un_decimal_de_escala()
        => Assert.Equal("1.0", PoliticaRedondeo.Presentar(1m).ToString(System.Globalization.CultureInfo.InvariantCulture));
}
