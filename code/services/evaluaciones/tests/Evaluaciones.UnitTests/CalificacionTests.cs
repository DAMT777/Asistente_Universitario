using Evaluaciones.Domain;
using Evaluaciones.Domain.Calificaciones;
using Evaluaciones.Domain.Cursos;
using Xunit;

namespace Evaluaciones.UnitTests;

public class CalificacionTests
{
    private static readonly Actividad ConEntrega = new(Guid.NewGuid(), Guid.NewGuid(), "Taller", 1, 20, null, true);
    private static readonly Actividad SinEntrega = new(Guid.NewGuid(), Guid.NewGuid(), "Parcial", 1, 80, null, false);

    private static Calificacion Nueva(Actividad a, decimal valor = 4.0m, string? retro = null, Guid? entrega = null) =>
        Calificacion.Crear(Guid.NewGuid(), a, Guid.NewGuid(), entrega, valor, retro);

    [Fact]
    public void Crear_nace_como_borrador()
    {
        var c = Nueva(ConEntrega, 4.5m, "Buen análisis");
        Assert.Equal(EstadoCalificacion.Borrador, c.Estado);
        Assert.Equal(4.5m, c.Valor);
        Assert.Equal("Buen análisis", c.Retroalimentacion);
    }

    [Fact]
    public void Cero_es_una_nota_valida()
    {
        var c = Nueva(SinEntrega, 0.0m);
        Assert.Equal(0.0m, c.Valor);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(5.1)]
    [InlineData(7)]
    public void Fuera_de_rango_se_rechaza(double valor)
    {
        var ex = Assert.Throws<NotaFueraDeRangoException>(() => Nueva(ConEntrega, (decimal)valor));
        Assert.Equal("NOTA_FUERA_DE_RANGO", ex.Codigo);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(5.0)]
    [InlineData(3.5)]
    public void Los_extremos_de_la_escala_se_aceptan(double valor) => Nueva(ConEntrega, (decimal)valor);

    [Fact]
    public void Mas_de_un_decimal_se_rechaza()
    {
        var ex = Assert.Throws<ValidacionFallidaException>(() => Nueva(ConEntrega, 3.55m));
        Assert.Equal("VALIDACION_FALLIDA", ex.Codigo);
    }

    [Fact]
    public void Retroalimentacion_vacia_se_guarda_como_nula_y_se_recorta()
    {
        Assert.Null(Nueva(ConEntrega, 3m, "   ").Retroalimentacion);
        Assert.Equal("ok", Nueva(ConEntrega, 3m, "  ok ").Retroalimentacion);
    }

    [Fact]
    public void Retroalimentacion_demasiado_larga_se_rechaza()
    {
        var larga = new string('x', Calificacion.LongitudMaximaRetroalimentacion + 1);
        Assert.Throws<ValidacionFallidaException>(() => Nueva(ConEntrega, 3m, larga));
    }

    [Fact]
    public void Una_actividad_sin_entrega_no_acepta_entregaId()
    {
        Assert.Throws<ValidacionFallidaException>(() => Nueva(SinEntrega, 3m, entrega: Guid.NewGuid()));
    }

    [Fact]
    public void Una_actividad_con_entrega_acepta_entregaId_y_tambien_nota_sin_entrega()
    {
        var entrega = Guid.NewGuid();
        Assert.Equal(entrega, Nueva(ConEntrega, 3m, entrega: entrega).EntregaId);
        Assert.Null(Nueva(ConEntrega, 0m).EntregaId);
    }

    [Fact]
    public void Modificar_una_nota_publicada_la_devuelve_a_borrador()
    {
        var c = Nueva(ConEntrega, 3.0m);
        c.Publicar();

        var cambio = c.Modificar(ConEntrega, null, 4.0m, "Tras el reclamo");

        Assert.True(cambio);
        Assert.Equal(4.0m, c.Valor);
        Assert.Equal(EstadoCalificacion.Borrador, c.Estado);
    }

    [Fact]
    public void Modificar_sin_cambios_no_toca_el_estado()
    {
        var c = Nueva(ConEntrega, 3.0m, "igual");
        c.Publicar();

        var cambio = c.Modificar(ConEntrega, null, 3.0m, "igual");

        Assert.False(cambio);
        Assert.Equal(EstadoCalificacion.Publicada, c.Estado);
    }

    [Fact]
    public void Modificar_sin_entregaId_conserva_el_vinculo()
    {
        var entrega = Guid.NewGuid();
        var c = Nueva(ConEntrega, 3.0m, entrega: entrega);
        c.Modificar(ConEntrega, null, 3.5m, null);
        Assert.Equal(entrega, c.EntregaId);
    }

    [Fact]
    public void Modificar_a_cero_es_una_nota_real()
    {
        var c = Nueva(SinEntrega, 3.0m);
        c.Modificar(SinEntrega, null, 0.0m, null);
        Assert.Equal(0.0m, c.Valor);
    }

    [Fact]
    public void Modificar_con_nota_invalida_no_cambia_nada()
    {
        var c = Nueva(ConEntrega, 3.0m);
        Assert.Throws<NotaFueraDeRangoException>(() => c.Modificar(ConEntrega, null, 6m, "x"));
        Assert.Equal(3.0m, c.Valor);
        Assert.Null(c.Retroalimentacion);
    }

    [Fact]
    public void Publicar_es_idempotente()
    {
        var c = Nueva(ConEntrega);
        Assert.True(c.Publicar());
        Assert.False(c.Publicar());
        Assert.Equal(EstadoCalificacion.Publicada, c.Estado);
    }
}
