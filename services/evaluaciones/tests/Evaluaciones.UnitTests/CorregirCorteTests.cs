using Evaluaciones.Application.CasosDeUso;
using Evaluaciones.Application.Dtos;
using Evaluaciones.Domain.Entidades;
using Evaluaciones.Domain.Errores;
using Evaluaciones.UnitTests.Apoyo;

namespace Evaluaciones.UnitTests;

/// <summary>CU-11. Escenario E-15 de la guía.</summary>
public class CorregirCorteTests
{
    private static CorregirCorte CrearCasoDeUso(Escenario e) =>
        new(e.Base, e.Base, e.Base, e.Base, e.Base, e.Reloj);

    private static Task<PublicacionCorteDto> Corregir(
        Escenario e, Guid estudianteId, int corte = 1, bool omitirBorradores = false,
        Guid? profesorId = null, byte[]? version = null) =>
        CrearCasoDeUso(e).EjecutarAsync(
            profesorId ?? Escenario.ProfesorId, Escenario.CursoId, corte, estudianteId,
            new SolicitudCorregirCorte(omitirBorradores), version, default);

    [Fact]
    public async Task Corrige_solo_la_publicacion_de_Ana_y_deja_intactas_las_demas()
    {
        var e = new Escenario();
        var deLuis = e.Publicar(Escenario.Luis, corte: 1, nota: 2.0m);
        e.CalificacionDe(Escenario.Ana, Escenario.Taller1).Valor = 5.0m; // reclamo: de 4.0 a 5.0

        var resultado = await Corregir(e, Escenario.Ana);

        Assert.Equal(3.4m, resultado.Nota); // 5.0 × 20 / 100 + 3.0 × 80 / 100
        Assert.Equal(3.4m, e.Base.Publicaciones.Single(p => p.EstudianteId == Escenario.Ana).Nota);
        Assert.Equal(2.0m, deLuis.Nota);
        Assert.Equal(2, e.Base.Publicaciones.Count);
        Assert.Equal(1, e.Base.GuardadosRealizados);
    }

    [Fact]
    public async Task Actualiza_la_fecha_de_publicacion()
    {
        var e = new Escenario();

        var resultado = await Corregir(e, Escenario.Ana);

        Assert.Equal(Escenario.Ahora.UtcDateTime, resultado.FechaPublicacion);
    }

    [Fact]
    public async Task Sin_publicacion_previa_recibe_NO_ENCONTRADO()
    {
        var e = new Escenario();

        var ex = await Assert.ThrowsAsync<DominioException>(() => Corregir(e, Escenario.Marta));

        Assert.Equal(CodigosError.NoEncontrado, ex.Codigo);
    }

    [Fact]
    public async Task Con_una_calificacion_modificada_en_borrador_se_rechaza()
    {
        // Si el profesor modifica la nota y queda en borrador, debe publicar la actividad antes (CU-08).
        var e = new Escenario();
        e.CalificacionDe(Escenario.Ana, Escenario.Taller1).Estado = EstadoCalificacion.Borrador;

        var ex = await Assert.ThrowsAsync<DominioException>(() => Corregir(e, Escenario.Ana));

        Assert.Equal(CodigosError.CalificacionesEnBorrador, ex.Codigo);
        Assert.Equal(3.2m, e.Base.Publicaciones.Single().Nota);
    }

    [Fact]
    public async Task Con_omitir_borradores_recalcula_solo_con_lo_publicado()
    {
        var e = new Escenario();
        e.CalificacionDe(Escenario.Ana, Escenario.Taller1).Estado = EstadoCalificacion.Borrador;

        var resultado = await Corregir(e, Escenario.Ana, omitirBorradores: true);

        Assert.Equal(2.4m, resultado.Nota); // solo el parcial publicado
    }

    [Fact]
    public async Task Otro_profesor_recibe_SIN_PERMISO()
    {
        var e = new Escenario();

        var ex = await Assert.ThrowsAsync<DominioException>(
            () => Corregir(e, Escenario.Ana, profesorId: Escenario.OtroProfesorId));

        Assert.Equal(CodigosError.SinPermiso, ex.Codigo);
    }

    [Fact]
    public async Task Corte_invalido_recibe_VALIDACION_FALLIDA()
    {
        var e = new Escenario();

        var ex = await Assert.ThrowsAsync<DominioException>(() => Corregir(e, Escenario.Ana, corte: 7));

        Assert.Equal(CodigosError.ValidacionFallida, ex.Codigo);
    }

    [Fact]
    public async Task Exige_la_version_cuando_el_cliente_la_envia()
    {
        var e = new Escenario();
        byte[] version = [1, 2, 3, 4, 5, 6, 7, 8];

        await Corregir(e, Escenario.Ana, version: version);

        Assert.Equal(version, e.Base.VersionExigida);
    }

    [Fact]
    public async Task Sin_encabezado_de_version_no_exige_nada()
    {
        var e = new Escenario();

        await Corregir(e, Escenario.Ana);

        Assert.Null(e.Base.VersionExigida);
    }
}
