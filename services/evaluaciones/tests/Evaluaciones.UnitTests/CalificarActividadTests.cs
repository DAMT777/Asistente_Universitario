using Evaluaciones.Application.CasosDeUso;
using Evaluaciones.Application.Dtos;
using Evaluaciones.Domain.Entidades;
using Evaluaciones.Domain.Errores;
using Evaluaciones.UnitTests.Apoyo;

namespace Evaluaciones.UnitTests;

/// <summary>CU-05, CU-06 y CU-07. Escenarios E-08, E-09, E-18 y E-19 de la guía.</summary>
public class CalificarActividadTests
{
    private static readonly Guid EntregaDeMarta = new("e0000000-0000-0000-0000-0000000000aa");

    private static Task<CalificacionDto> Calificar(
        Escenario e, Guid estudianteId, Guid actividadId, decimal? valor, string? retro = null,
        Guid? entregaId = null, Guid? profesorId = null, byte[]? version = null) =>
        new CalificarActividad(e.Base, e.Base, e.Base, e.Base, e.Base).EjecutarAsync(
            profesorId ?? Escenario.ProfesorId, actividadId, estudianteId,
            new SolicitudCalificar(valor, retro, entregaId), version, default);

    // ───────── CU-05: calificar una entrega ─────────

    [Fact]
    public async Task CU05_califica_la_entrega_con_nota_y_retroalimentacion_y_queda_en_borrador()
    {
        var e = new Escenario();

        var dto = await Calificar(e, Escenario.Marta, Escenario.Taller1, 4.5m,
            "  Buen análisis, falta justificar el supuesto 2.  ", EntregaDeMarta);

        var guardada = e.CalificacionDe(Escenario.Marta, Escenario.Taller1);
        Assert.Equal(4.5m, guardada.Valor);
        Assert.Equal("Buen análisis, falta justificar el supuesto 2.", guardada.Retroalimentacion);
        Assert.Equal(EntregaDeMarta, guardada.EntregaId);
        Assert.Equal(EstadoCalificacion.Borrador, guardada.Estado);
        Assert.Equal("BORRADOR", dto.Estado);
        Assert.Equal(guardada.Id, dto.Id);
        Assert.Equal(1, e.Base.GuardadosRealizados);
    }

    [Fact]
    public async Task CU05_el_borrador_no_es_visible_para_el_estudiante()
    {
        // E-08, antes de publicar.
        var e = new Escenario();

        await Calificar(e, Escenario.Marta, Escenario.Taller1, 4.5m);

        var visibles = await e.Base.ListarCalificacionesPublicadasAsync(Escenario.Marta, default);
        Assert.Empty(visibles);
    }

    // ───────── CU-06: actividad sin entrega ─────────

    [Fact]
    public async Task CU06_registra_la_nota_del_parcial_sin_entrega()
    {
        var e = new Escenario();

        var dto = await Calificar(e, Escenario.Marta, Escenario.Parcial1, 3.8m, "Sustentación clara.");

        Assert.Equal(3.8m, dto.Valor);
        Assert.Null(dto.EntregaId);
        Assert.Equal("BORRADOR", dto.Estado);
    }

    [Fact]
    public async Task CU06_indicar_una_entrega_en_una_actividad_sin_entrega_responde_ACTIVIDAD_SIN_ENTREGA()
    {
        var e = new Escenario();

        var ex = await Assert.ThrowsAsync<DominioException>(
            () => Calificar(e, Escenario.Marta, Escenario.Parcial1, 3.8m, entregaId: EntregaDeMarta));

        Assert.Equal(CodigosError.ActividadSinEntrega, ex.Codigo);
        Assert.Equal(0, e.Base.GuardadosRealizados);
    }

    [Fact]
    public async Task CU06_un_cero_es_una_calificacion_y_no_queda_sin_calificar()
    {
        // E-09
        var e = new Escenario();

        await Calificar(e, Escenario.Marta, Escenario.Parcial1, 0.0m);

        Assert.Equal(0.0m, e.CalificacionDe(Escenario.Marta, Escenario.Parcial1).Valor);
        Assert.DoesNotContain(e.Base.Calificaciones, c => c.EstudianteId == Escenario.Marta && c.ActividadId == Escenario.Taller1);
    }

    // ───────── CU-07: modificar una nota ─────────

    [Fact]
    public async Task CU07_modificar_una_nota_publicada_la_cambia_y_vuelve_a_borrador()
    {
        var e = new Escenario();
        var antes = e.CalificacionDe(Escenario.Ana, Escenario.Taller1);

        var dto = await Calificar(e, Escenario.Ana, Escenario.Taller1, 4.6m, "Se revisó el reclamo.");

        Assert.Equal(antes.Id, dto.Id); // misma fila, no se duplica
        Assert.Equal(4.6m, antes.Valor);
        Assert.Equal("Se revisó el reclamo.", antes.Retroalimentacion);
        Assert.Equal(EstadoCalificacion.Borrador, antes.Estado);
        Assert.Single(e.Base.Calificaciones, c => c.EstudianteId == Escenario.Ana && c.ActividadId == Escenario.Taller1);
    }

    [Fact]
    public async Task CU07_guardar_sin_cambios_conserva_el_estado_y_no_escribe()
    {
        var e = new Escenario();
        var ana = e.CalificacionDe(Escenario.Ana, Escenario.Parcial1); // 3.0 publicada, retro vacía

        var dto = await Calificar(e, Escenario.Ana, Escenario.Parcial1, 3.0m, "");

        Assert.Equal("PUBLICADA", dto.Estado);
        Assert.Equal(EstadoCalificacion.Publicada, ana.Estado);
        Assert.Equal(0, e.Base.GuardadosRealizados);
    }

    [Fact]
    public async Task CU07_si_no_se_envia_la_entrega_conserva_la_que_tenia()
    {
        var e = new Escenario();
        var ana = e.CalificacionDe(Escenario.Ana, Escenario.Taller1);
        ana.EntregaId = EntregaDeMarta;

        await Calificar(e, Escenario.Ana, Escenario.Taller1, 3.5m);

        Assert.Equal(EntregaDeMarta, ana.EntregaId);
    }

    [Fact]
    public async Task CU07_exige_la_version_cuando_el_cliente_la_envia()
    {
        // E-19: la unidad de trabajo responde 409 si la versión no coincide.
        var e = new Escenario();
        byte[] version = [1, 2, 3, 4, 5, 6, 7, 8];

        await Calificar(e, Escenario.Ana, Escenario.Taller1, 4.1m, version: version);

        Assert.Equal(version, e.Base.VersionExigidaCalificacion);
    }

    [Fact]
    public async Task CU07_enviar_version_de_una_calificacion_que_no_existe_responde_CONFLICTO()
    {
        var e = new Escenario();

        var ex = await Assert.ThrowsAsync<DominioException>(
            () => Calificar(e, Escenario.Marta, Escenario.Parcial1, 4.0m, version: [1, 2, 3]));

        Assert.Equal(CodigosError.ConflictoConcurrencia, ex.Codigo);
    }

    // ───────── Validaciones y permisos ─────────

    [Theory]
    [InlineData(-0.1)]
    [InlineData(5.1)]
    [InlineData(10)]
    public async Task Nota_fuera_de_0_a_5_responde_NOTA_FUERA_DE_RANGO(double valor)
    {
        var e = new Escenario();

        var ex = await Assert.ThrowsAsync<DominioException>(
            () => Calificar(e, Escenario.Marta, Escenario.Parcial1, (decimal)valor));

        Assert.Equal(CodigosError.NotaFueraDeRango, ex.Codigo);
    }

    [Fact]
    public async Task Nota_con_dos_decimales_responde_VALIDACION_FALLIDA()
    {
        var e = new Escenario();

        var ex = await Assert.ThrowsAsync<DominioException>(() => Calificar(e, Escenario.Marta, Escenario.Parcial1, 3.45m));

        Assert.Equal(CodigosError.ValidacionFallida, ex.Codigo);
    }

    [Fact]
    public async Task Sin_nota_responde_VALIDACION_FALLIDA()
    {
        var e = new Escenario();

        var ex = await Assert.ThrowsAsync<DominioException>(() => Calificar(e, Escenario.Marta, Escenario.Parcial1, null));

        Assert.Equal(CodigosError.ValidacionFallida, ex.Codigo);
    }

    [Fact]
    public async Task Retroalimentacion_de_mas_de_2000_caracteres_responde_VALIDACION_FALLIDA()
    {
        var e = new Escenario();

        var ex = await Assert.ThrowsAsync<DominioException>(
            () => Calificar(e, Escenario.Marta, Escenario.Parcial1, 3.0m, new string('x', 2001)));

        Assert.Equal(CodigosError.ValidacionFallida, ex.Codigo);
    }

    [Fact]
    public async Task Otro_profesor_recibe_SIN_PERMISO()
    {
        // E-18
        var e = new Escenario();

        var ex = await Assert.ThrowsAsync<DominioException>(
            () => Calificar(e, Escenario.Marta, Escenario.Parcial1, 3.0m, profesorId: Escenario.OtroProfesorId));

        Assert.Equal(CodigosError.SinPermiso, ex.Codigo);
    }

    [Fact]
    public async Task Actividad_inexistente_responde_NO_ENCONTRADO()
    {
        var e = new Escenario();

        var ex = await Assert.ThrowsAsync<DominioException>(() => Calificar(e, Escenario.Marta, Guid.NewGuid(), 3.0m));

        Assert.Equal(CodigosError.NoEncontrado, ex.Codigo);
    }

    [Fact]
    public async Task Estudiante_no_inscrito_responde_NO_ENCONTRADO()
    {
        var e = new Escenario();

        var ex = await Assert.ThrowsAsync<DominioException>(() => Calificar(e, Guid.NewGuid(), Escenario.Parcial1, 3.0m));

        Assert.Equal(CodigosError.NoEncontrado, ex.Codigo);
        Assert.Equal(0, e.Base.GuardadosRealizados);
    }
}
