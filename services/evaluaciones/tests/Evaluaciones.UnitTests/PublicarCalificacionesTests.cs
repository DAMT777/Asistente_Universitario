using Evaluaciones.Application.CasosDeUso;
using Evaluaciones.Domain.Entidades;
using Evaluaciones.Domain.Errores;
using Evaluaciones.UnitTests.Apoyo;

namespace Evaluaciones.UnitTests;

/// <summary>CU-08 y lo que ve el estudiante después (CU-15, CU-16). Escenario E-08 de la guía.</summary>
public class PublicarCalificacionesTests
{
    private static PublicarCalificacionesDeActividad Publicar(Escenario e) => new(e.Base, e.Base, e.Base, e.Base);
    private static ObtenerNotasActividades NotasActividades(Escenario e) => new(e.Base, e.Base, e.Base, e.Base);
    private static ObtenerMatrizNotas Matriz(Escenario e) => new(e.Base, e.Base);

    private static async Task<List<Evaluaciones.Application.Dtos.NotaActividadDto>> PublicadasAsync(Escenario e, Guid estudianteId) =>
        (await NotasActividades(e).EjecutarAsync(estudianteId, Escenario.CursoId, default)).Actividades
            .Where(a => a.Estado == ObtenerNotasActividades.Publicada).ToList();

    [Fact]
    public async Task CU08_publica_los_borradores_de_la_actividad_y_no_toca_otras()
    {
        var e = new Escenario();
        var marta = e.Calificar(Escenario.Marta, Escenario.Taller1, 3.5m, EstadoCalificacion.Borrador);
        var otraActividad = e.Calificar(Escenario.Marta, Escenario.Parcial1, 4.0m, EstadoCalificacion.Borrador);

        var resultado = await Publicar(e).EjecutarAsync(Escenario.ProfesorId, Escenario.Taller1, default);

        Assert.Equal(2, resultado.Publicadas); // Luis y Marta
        Assert.Equal(Escenario.Taller1, resultado.ActividadId);
        Assert.Equal(EstadoCalificacion.Publicada, marta.Estado);
        Assert.Equal(EstadoCalificacion.Publicada, e.CalificacionDe(Escenario.Luis, Escenario.Taller1).Estado);
        Assert.Equal(EstadoCalificacion.Borrador, otraActividad.Estado);
        Assert.Equal(1, e.Base.GuardadosRealizados);
    }

    [Fact]
    public async Task CU08_sin_borradores_responde_cero_y_no_escribe()
    {
        var e = new Escenario();

        var resultado = await Publicar(e).EjecutarAsync(Escenario.ProfesorId, Escenario.Parcial1, default);

        Assert.Equal(0, resultado.Publicadas);
        Assert.Equal(0, e.Base.GuardadosRealizados);
    }

    [Fact]
    public async Task CU08_publicar_no_publica_el_corte()
    {
        var e = new Escenario();

        await Publicar(e).EjecutarAsync(Escenario.ProfesorId, Escenario.Taller1, default);

        Assert.DoesNotContain(e.Base.Publicaciones, p => p.EstudianteId == Escenario.Luis);
    }

    [Fact]
    public async Task CU08_otro_profesor_recibe_SIN_PERMISO()
    {
        var e = new Escenario();

        var ex = await Assert.ThrowsAsync<DominioException>(
            () => Publicar(e).EjecutarAsync(Escenario.OtroProfesorId, Escenario.Taller1, default));

        Assert.Equal(CodigosError.SinPermiso, ex.Codigo);
        Assert.Equal(EstadoCalificacion.Borrador, e.CalificacionDe(Escenario.Luis, Escenario.Taller1).Estado);
    }

    [Fact]
    public async Task CU08_actividad_inexistente_responde_NO_ENCONTRADO()
    {
        var e = new Escenario();

        var ex = await Assert.ThrowsAsync<DominioException>(
            () => Publicar(e).EjecutarAsync(Escenario.ProfesorId, Guid.NewGuid(), default));

        Assert.Equal(CodigosError.NoEncontrado, ex.Codigo);
    }

    [Fact]
    public async Task E08_el_estudiante_ve_la_nota_y_la_retroalimentacion_solo_despues_de_publicar()
    {
        var e = new Escenario();
        await new CalificarActividad(e.Base, e.Base, e.Base, e.Base, e.Base).EjecutarAsync(
            Escenario.ProfesorId, Escenario.Parcial1, Escenario.Marta,
            new(4.5m, "Muy bien."), null, default);

        var antes = await PublicadasAsync(e, Escenario.Marta);
        await Publicar(e).EjecutarAsync(Escenario.ProfesorId, Escenario.Parcial1, default);
        var despues = await PublicadasAsync(e, Escenario.Marta);

        Assert.Empty(antes);
        var nota = Assert.Single(despues);
        Assert.Equal(Escenario.Parcial1, nota.ActividadId);
        Assert.Equal(4.5m, nota.Nota);
        Assert.Equal("Muy bien.", nota.Retroalimentacion);
        Assert.Equal("PUBLICADA", nota.Estado);
    }

    [Fact]
    public async Task CU15_el_estudiante_no_ve_sus_borradores()
    {
        var e = new Escenario();

        var deLuis = await PublicadasAsync(e, Escenario.Luis);

        Assert.Empty(deLuis);
    }

    [Fact]
    public async Task CU15_un_estudiante_no_inscrito_recibe_SIN_PERMISO()
    {
        var e = new Escenario();

        var ex = await Assert.ThrowsAsync<DominioException>(
            () => NotasActividades(e).EjecutarAsync(Guid.NewGuid(), Escenario.CursoId, default));

        Assert.Equal(CodigosError.SinPermiso, ex.Codigo);
    }

    [Fact]
    public async Task CU16_matriz_de_Ana_con_corte_1_publicado_y_definitiva_parcial()
    {
        var e = new Escenario();

        var matriz = await Matriz(e).EjecutarAsync(Escenario.Ana, default);

        var curso = Assert.Single(matriz.Cursos);
        Assert.Equal(3.2m, curso.Cortes[0].Nota);
        Assert.True(curso.Cortes[0].Publicado);
        Assert.Null(curso.Cortes[1].Nota); // sin publicar no es 0 (RN-14)
        Assert.False(curso.Cortes[1].Publicado);
        Assert.Equal(1.0m, curso.DefinitivaParcial); // 3.2 × 30 / 100 = 0.96
        Assert.True(curso.EsParcial);
    }

    [Fact]
    public async Task CU16_estudiante_sin_publicaciones_ve_cortes_nulos_y_definitiva_0()
    {
        // E-14
        var e = new Escenario();

        var curso = Assert.Single((await Matriz(e).EjecutarAsync(Escenario.Marta, default)).Cursos);

        Assert.All(curso.Cortes, k => Assert.Null(k.Nota));
        Assert.Equal(0.0m, curso.DefinitivaParcial);
        Assert.True(curso.EsParcial);
    }
}
