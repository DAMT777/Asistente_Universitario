using Evaluaciones.Application.CasosDeUso;
using Evaluaciones.Domain.Entidades;
using Evaluaciones.Domain.Errores;
using Evaluaciones.UnitTests.Apoyo;

namespace Evaluaciones.UnitTests;

/// <summary>CU-09. Escenarios E-09 y E-13 de la guía.</summary>
public class ConsultarPonderadoTests
{
    private static ConsultarPonderado CrearCasoDeUso(Escenario e) =>
        new(e.Base, e.Base, e.Base, e.Base, e.Base);

    [Fact]
    public async Task Ponderado_de_Ana_tiene_corte_1_en_3_2_y_definitiva_parcial_1_0()
    {
        // E-13
        var e = new Escenario();

        var ana = await CrearCasoDeUso(e).PorEstudianteAsync(
            Escenario.ProfesorId, Escenario.CursoId, Escenario.Ana, default);

        var corte1 = ana.Cortes.Single(c => c.Corte == 1);
        Assert.Equal(3.2m, corte1.NotaCorte);
        Assert.Equal(30m, corte1.PesoCorte);
        Assert.True(corte1.Publicado);
        Assert.Equal(3.2m, corte1.NotaPublicada);
        Assert.Equal(1.0m, ana.DefinitivaParcial);
        Assert.True(ana.EsParcial);
        Assert.False(ana.IncluyeBorradores);
    }

    [Fact]
    public async Task Cortes_sin_calificaciones_salen_con_nota_nula_y_no_en_cero()
    {
        var e = new Escenario();

        var ana = await CrearCasoDeUso(e).PorEstudianteAsync(
            Escenario.ProfesorId, Escenario.CursoId, Escenario.Ana, default);

        foreach (var corte in ana.Cortes.Where(c => c.Corte != 1))
        {
            Assert.Null(corte.NotaCorte);
            Assert.False(corte.Publicado);
            Assert.Null(corte.NotaPublicada);
        }
    }

    [Fact]
    public async Task Detalle_por_actividad_muestra_valor_estado_y_aporte()
    {
        var e = new Escenario();

        var ana = await CrearCasoDeUso(e).PorEstudianteAsync(
            Escenario.ProfesorId, Escenario.CursoId, Escenario.Ana, default);

        var actividades = ana.Cortes.Single(c => c.Corte == 1).Actividades!;
        var taller = actividades.Single(a => a.ActividadId == Escenario.Taller1);
        Assert.Equal(4.0m, taller.Valor);
        Assert.Equal("PUBLICADA", taller.Estado);
        Assert.Equal(0.8m, taller.Aporte);
    }

    [Fact]
    public async Task Marta_sin_calificaciones_tiene_definitiva_cero_y_es_parcial()
    {
        var e = new Escenario();

        var marta = await CrearCasoDeUso(e).PorEstudianteAsync(
            Escenario.ProfesorId, Escenario.CursoId, Escenario.Marta, default);

        Assert.All(marta.Cortes, c => Assert.Null(c.NotaCorte));
        Assert.Equal(0.0m, marta.DefinitivaParcial);
        Assert.True(marta.EsParcial);
    }

    [Fact]
    public async Task Distingue_calificada_con_cero_de_sin_calificar()
    {
        // E-09
        var e = new Escenario();
        e.Calificar(Escenario.Marta, Escenario.Taller1, 0.0m, EstadoCalificacion.Publicada);

        var marta = await CrearCasoDeUso(e).PorEstudianteAsync(
            Escenario.ProfesorId, Escenario.CursoId, Escenario.Marta, default);

        var corte1 = marta.Cortes.Single(c => c.Corte == 1);
        Assert.Equal(0.0m, corte1.NotaCorte); // hay una calificación, así que no es nulo
        var actividades = corte1.Actividades!;
        Assert.Equal(0.0m, actividades.Single(a => a.ActividadId == Escenario.Taller1).Valor);
        Assert.Null(actividades.Single(a => a.ActividadId == Escenario.Parcial1).Valor);
        Assert.Null(actividades.Single(a => a.ActividadId == Escenario.Parcial1).Estado);
    }

    [Fact]
    public async Task El_profesor_ve_los_borradores_incluidos_y_marcados()
    {
        var e = new Escenario();

        var luis = await CrearCasoDeUso(e).PorEstudianteAsync(
            Escenario.ProfesorId, Escenario.CursoId, Escenario.Luis, default);

        Assert.True(luis.IncluyeBorradores);
        Assert.Equal(0.5m, luis.Cortes.Single(c => c.Corte == 1).NotaCorte); // 2.5 × 20 / 100
        Assert.False(luis.Cortes.Single(c => c.Corte == 1).Publicado);
    }

    [Fact]
    public async Task Tabla_del_curso_lista_a_todos_ordenados_por_nombre_y_sin_detalle()
    {
        var e = new Escenario();

        var curso = await CrearCasoDeUso(e).PorCursoAsync(Escenario.ProfesorId, Escenario.CursoId, default);

        Assert.Equal("Simulación computacional", curso.Curso);
        Assert.Equal(new[] { "Ana Demo", "Luis Demo", "Marta Demo" }, curso.Estudiantes.Select(s => s.Nombre));
        Assert.All(curso.Estudiantes, s => Assert.All(s.Cortes, c => Assert.Null(c.Actividades)));
        Assert.Equal(1.0m, curso.Estudiantes.Single(s => s.EstudianteId == Escenario.Ana).DefinitivaParcial);
    }

    [Fact]
    public async Task Otro_profesor_recibe_SIN_PERMISO()
    {
        var e = new Escenario();

        var ex = await Assert.ThrowsAsync<DominioException>(() => CrearCasoDeUso(e).PorCursoAsync(
            Escenario.OtroProfesorId, Escenario.CursoId, default));

        Assert.Equal(CodigosError.SinPermiso, ex.Codigo);
    }

    [Fact]
    public async Task Estudiante_no_inscrito_recibe_NO_ENCONTRADO()
    {
        var e = new Escenario();

        var ex = await Assert.ThrowsAsync<DominioException>(() => CrearCasoDeUso(e).PorEstudianteAsync(
            Escenario.ProfesorId, Escenario.CursoId, Guid.NewGuid(), default));

        Assert.Equal(CodigosError.NoEncontrado, ex.Codigo);
    }
}
