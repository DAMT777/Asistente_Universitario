using Evaluaciones.Application.CasosDeUso;
using Evaluaciones.Application.Dtos;
using Evaluaciones.Domain.Entidades;
using Evaluaciones.Domain.Errores;
using Evaluaciones.UnitTests.Apoyo;

namespace Evaluaciones.UnitTests;

/// <summary>CU-10. Escenarios E-12 y E-18 de la guía.</summary>
public class PublicarCorteTests
{
    private static PublicarCorte CrearCasoDeUso(Escenario e) =>
        new(e.Base, e.Base, e.Base, e.Base, e.Base, e.Base, e.Reloj);

    [Fact]
    public async Task Publica_a_Ana_con_nota_3_2_y_rechaza_a_Luis_y_a_Marta()
    {
        var e = new Escenario(anaConCorte1Publicado: false);

        var resultado = await CrearCasoDeUso(e)
            .EjecutarAsync(Escenario.ProfesorId, Escenario.CursoId, 1, new SolicitudPublicarCorte(), default);

        var publicado = Assert.Single(resultado.Publicados);
        Assert.Equal(Escenario.Ana, publicado.EstudianteId);
        Assert.Equal(3.2m, publicado.Nota);

        // Luis tiene un borrador (E-12) y Marta no tiene calificaciones.
        Assert.Equal(2, resultado.Rechazados.Count);
        Assert.Equal(
            CodigosError.CalificacionesEnBorrador,
            resultado.Rechazados.Single(r => r.EstudianteId == Escenario.Luis).Codigo);
        Assert.Equal(
            CodigosError.SinCalificaciones,
            resultado.Rechazados.Single(r => r.EstudianteId == Escenario.Marta).Codigo);

        var guardada = Assert.Single(e.Base.Publicaciones);
        Assert.Equal(Escenario.Ana, guardada.EstudianteId);
        Assert.Equal(Escenario.Ahora.UtcDateTime, guardada.FechaPublicacion);
        Assert.Equal(1, e.Base.GuardadosRealizados);
    }

    [Fact]
    public async Task Con_omitir_borradores_publica_lo_que_ya_esta_publicado()
    {
        // E-12: con omitirBorradores en verdadero se publica con lo publicado.
        var e = new Escenario(anaConCorte1Publicado: false);
        e.Calificar(Escenario.Luis, Escenario.Parcial1, 3.0m, EstadoCalificacion.Publicada); // Taller sigue en borrador

        var resultado = await CrearCasoDeUso(e).EjecutarAsync(
            Escenario.ProfesorId, Escenario.CursoId, 1,
            new SolicitudPublicarCorte([Escenario.Luis], OmitirBorradores: true), default);

        var publicado = Assert.Single(resultado.Publicados);
        Assert.Equal(2.4m, publicado.Nota); // 3.0 × 80 / 100, el borrador del taller no cuenta
        Assert.Empty(resultado.Rechazados);
    }

    [Fact]
    public async Task Con_omitir_borradores_pero_solo_borradores_se_rechaza_por_sin_calificaciones()
    {
        var e = new Escenario(anaConCorte1Publicado: false);

        var resultado = await CrearCasoDeUso(e).EjecutarAsync(
            Escenario.ProfesorId, Escenario.CursoId, 1,
            new SolicitudPublicarCorte([Escenario.Luis], OmitirBorradores: true), default);

        Assert.Empty(resultado.Publicados);
        Assert.Equal(CodigosError.SinCalificaciones, Assert.Single(resultado.Rechazados).Codigo);
        Assert.Empty(e.Base.Publicaciones);
    }

    [Fact]
    public async Task Publicar_solo_a_los_estudiantes_indicados()
    {
        var e = new Escenario(anaConCorte1Publicado: false);

        var resultado = await CrearCasoDeUso(e).EjecutarAsync(
            Escenario.ProfesorId, Escenario.CursoId, 1, new SolicitudPublicarCorte([Escenario.Ana]), default);

        Assert.Single(resultado.Publicados);
        Assert.Empty(resultado.Rechazados);
    }

    [Fact]
    public async Task Publicar_de_nuevo_actualiza_la_misma_fila_sin_duplicar()
    {
        var e = new Escenario(anaConCorte1Publicado: true);
        var existente = e.Base.Publicaciones.Single();
        existente.Nota = 1.0m; // valor desactualizado

        await CrearCasoDeUso(e).EjecutarAsync(
            Escenario.ProfesorId, Escenario.CursoId, 1, new SolicitudPublicarCorte([Escenario.Ana]), default);

        var unica = Assert.Single(e.Base.Publicaciones);
        Assert.Same(existente, unica);
        Assert.Equal(3.2m, unica.Nota);
    }

    [Fact]
    public async Task Estudiante_no_inscrito_se_rechaza_sin_afectar_a_los_demas()
    {
        var e = new Escenario(anaConCorte1Publicado: false);
        var extrano = Guid.NewGuid();

        var resultado = await CrearCasoDeUso(e).EjecutarAsync(
            Escenario.ProfesorId, Escenario.CursoId, 1,
            new SolicitudPublicarCorte([extrano, Escenario.Ana]), default);

        Assert.Equal(Escenario.Ana, Assert.Single(resultado.Publicados).EstudianteId);
        var rechazado = Assert.Single(resultado.Rechazados);
        Assert.Equal(extrano, rechazado.EstudianteId);
        Assert.Equal(CodigosError.NoEncontrado, rechazado.Codigo);
    }

    [Fact]
    public async Task Otro_profesor_recibe_SIN_PERMISO()
    {
        // E-18
        var e = new Escenario();

        var ex = await Assert.ThrowsAsync<DominioException>(() => CrearCasoDeUso(e).EjecutarAsync(
            Escenario.OtroProfesorId, Escenario.CursoId, 1, new SolicitudPublicarCorte(), default));

        Assert.Equal(CodigosError.SinPermiso, ex.Codigo);
    }

    [Fact]
    public async Task Curso_inexistente_recibe_NO_ENCONTRADO()
    {
        var e = new Escenario();

        var ex = await Assert.ThrowsAsync<DominioException>(() => CrearCasoDeUso(e).EjecutarAsync(
            Escenario.ProfesorId, Guid.NewGuid(), 1, new SolicitudPublicarCorte(), default));

        Assert.Equal(CodigosError.NoEncontrado, ex.Codigo);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(-1)]
    public async Task Corte_fuera_de_1_a_3_recibe_VALIDACION_FALLIDA(int corte)
    {
        var e = new Escenario();

        var ex = await Assert.ThrowsAsync<DominioException>(() => CrearCasoDeUso(e).EjecutarAsync(
            Escenario.ProfesorId, Escenario.CursoId, corte, new SolicitudPublicarCorte(), default));

        Assert.Equal(CodigosError.ValidacionFallida, ex.Codigo);
    }
}
