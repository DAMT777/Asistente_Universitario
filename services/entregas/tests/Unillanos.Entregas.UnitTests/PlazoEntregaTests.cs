using Unillanos.Entregas.Domain;

namespace Unillanos.Entregas.UnitTests;

public sealed class PlazoEntregaTests
{
    private static readonly DateTimeOffset Limite = new(2026, 10, 15, 23, 59, 59, TimeSpan.Zero);

    [Fact]
    public void Justo_en_la_fecha_limite_esta_vigente()
    {
        var plazo = new PlazoEntrega(Limite);

        Assert.True(plazo.EstaVigente(Limite));
        plazo.ExigirVigente(Limite);
    }

    [Fact]
    public void Un_segundo_despues_del_limite_esta_vencido()
    {
        var plazo = new PlazoEntrega(Limite);
        var despues = Limite.AddSeconds(1);

        Assert.False(plazo.EstaVigente(despues));
        Assert.Throws<FechaLimiteVencidaException>(() => plazo.ExigirVigente(despues));
    }

    [Fact]
    public void Compara_en_UTC_aunque_ahora_venga_con_otra_zona()
    {
        var plazo = new PlazoEntrega(Limite);
        var colombia = TimeSpan.FromHours(-5);

        Assert.True(plazo.EstaVigente(new DateTimeOffset(2026, 10, 15, 18, 59, 59, colombia)));
        Assert.False(plazo.EstaVigente(new DateTimeOffset(2026, 10, 15, 19, 0, 0, colombia)));
    }

    [Fact]
    public void Actividad_sin_entrega_se_rechaza_aunque_este_vigente()
    {
        var ex = Assert.Throws<ActividadSinEntregaException>(
            () => PoliticaSubida.Verificar(requiereEntrega: false, new PlazoEntrega(Limite), Limite.AddDays(-1)));
        Assert.Equal("ACTIVIDAD_SIN_ENTREGA", ex.Codigo);
    }

    [Fact]
    public void Actividad_con_entrega_vencida_se_rechaza_con_fecha_limite_vencida()
    {
        var ex = Assert.Throws<FechaLimiteVencidaException>(
            () => PoliticaSubida.Verificar(requiereEntrega: true, new PlazoEntrega(Limite), Limite.AddSeconds(1)));
        Assert.Equal("FECHA_LIMITE_VENCIDA", ex.Codigo);
    }

    [Fact]
    public void Actividad_con_entrega_en_el_limite_se_acepta()
        => PoliticaSubida.Verificar(requiereEntrega: true, new PlazoEntrega(Limite), Limite);
}
