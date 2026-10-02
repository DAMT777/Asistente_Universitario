using Evaluaciones.Domain.Entidades;
using Evaluaciones.Domain.Errores;
using Evaluaciones.Domain.Ponderado;
using Evaluaciones.UnitTests.Apoyo;

namespace Evaluaciones.UnitTests;

public class MotorPonderadoTests
{
    private readonly Escenario _e = new();

    private IReadOnlyCollection<Actividad> Corte1 =>
        _e.Base.Actividades.Where(a => a.Corte == 1).ToList();

    [Fact]
    public void Nota_del_corte_de_Ana_es_3_2()
    {
        // E-13: Taller 4.0 × 20 / 100 + Parcial 3.0 × 80 / 100
        var valores = new Dictionary<Guid, decimal> { [Escenario.Taller1] = 4.0m, [Escenario.Parcial1] = 3.0m };

        Assert.Equal(3.2m, MotorPonderado.NotaCorte(Corte1, valores));
    }

    [Fact]
    public void Actividad_sin_calificar_no_aporta_y_no_se_trata_como_cero()
    {
        // Si el parcial aún no está calificado, el corte vale 0.8 hasta ese momento.
        var valores = new Dictionary<Guid, decimal> { [Escenario.Taller1] = 4.0m };

        Assert.Equal(0.8m, MotorPonderado.NotaCorte(Corte1, valores));
    }

    [Fact]
    public void Actividad_calificada_con_cero_aporta_cero()
    {
        // E-09: 0.0 es una calificación, no ausencia de calificación.
        var valores = new Dictionary<Guid, decimal> { [Escenario.Taller1] = 0.0m, [Escenario.Parcial1] = 3.0m };

        Assert.Equal(2.4m, MotorPonderado.NotaCorte(Corte1, valores));
    }

    [Fact]
    public void Definitiva_parcial_de_Ana_es_1_0()
    {
        // 3.2 × 30 / 100 = 0.96, que se muestra como 1.0 (DA-01).
        var curso = _e.Base.Cursos.Single();

        var definitiva = MotorPonderado.DefinitivaParcial(curso, new Dictionary<int, decimal> { [1] = 3.2m });

        Assert.Equal(1.0m, definitiva);
    }

    [Fact]
    public void Definitiva_sin_cortes_es_cero()
    {
        var curso = _e.Base.Cursos.Single();

        Assert.Equal(0.0m, MotorPonderado.DefinitivaParcial(curso, new Dictionary<int, decimal>()));
    }

    [Theory]
    [InlineData(0.25, 0.3)]
    [InlineData(0.24, 0.2)]
    [InlineData(3.25, 3.3)]
    [InlineData(4.95, 5.0)]
    public void Redondear_a_un_decimal_con_mitad_hacia_arriba(double entrada, double esperado) =>
        Assert.Equal((decimal)esperado, MotorPonderado.Redondear((decimal)entrada));

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(3, true)]
    [InlineData(4, false)]
    public void Solo_existen_los_cortes_1_a_3(int corte, bool esperado) =>
        Assert.Equal(esperado, MotorPonderado.EsCorteValido(corte));

    [Fact]
    public void Nota_publicable_ignora_las_calificaciones_de_otros_cortes()
    {
        // Ana tiene su corte 1 completo; consultar el corte 2 no debe mezclarlos.
        var corte2 = _e.Base.Actividades.Where(a => a.Corte == 2).ToList();
        var deAna = _e.Base.Calificaciones.Where(c => c.EstudianteId == Escenario.Ana);

        var ex = Assert.Throws<DominioException>(() => MotorPonderado.NotaCortePublicable(corte2, deAna, false));

        Assert.Equal(CodigosError.SinCalificaciones, ex.Codigo);
    }

    [Fact]
    public void Nota_publicable_con_borradores_pluraliza_el_mensaje()
    {
        _e.Calificar(Escenario.Marta, Escenario.Taller1, 1.0m, EstadoCalificacion.Borrador);
        _e.Calificar(Escenario.Marta, Escenario.Parcial1, 1.0m, EstadoCalificacion.Borrador);
        var deMarta = _e.Base.Calificaciones.Where(c => c.EstudianteId == Escenario.Marta);

        var ex = Assert.Throws<DominioException>(() => MotorPonderado.NotaCortePublicable(Corte1, deMarta, false));

        Assert.Equal(CodigosError.CalificacionesEnBorrador, ex.Codigo);
        Assert.Equal("Tiene 2 calificaciones en borrador.", ex.Message);
    }
}
