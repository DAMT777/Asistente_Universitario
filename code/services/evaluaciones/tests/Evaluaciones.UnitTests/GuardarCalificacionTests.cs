using Evaluaciones.Application;
using Evaluaciones.Application.Calificaciones;
using Evaluaciones.Domain;
using Evaluaciones.Domain.Calificaciones;
using Xunit;
using static Evaluaciones.UnitTests.BaseEnMemoria;

namespace Evaluaciones.UnitTests;

public class GuardarCalificacionTests
{
    private readonly BaseEnMemoria bd = new();
    private static readonly Actor Profesor = new(ProfesorId, Roles.Profesor);

    private GuardarCalificacion Caso() => new(bd, bd, bd);

    private static GuardarCalificacionComando Cmd(Guid actividad, Guid estudiante, decimal? valor, string? retro = null,
        Guid? entrega = null, string? version = null, Actor? actor = null) =>
        new(actor ?? Profesor, actividad, estudiante, valor, retro, entrega, version);

    // CU-05: calificar una entrega con nota y retroalimentación
    [Fact]
    public async Task Califica_una_entrega_con_nota_y_retroalimentacion_y_queda_en_borrador()
    {
        var entrega = Guid.NewGuid();
        var dto = await Caso().EjecutarAsync(Cmd(bd.Taller.Id, Ana, 4.5m, "Falta justificar el supuesto 2.", entrega), default);

        Assert.Equal(4.5m, dto.Valor);
        Assert.Equal("Falta justificar el supuesto 2.", dto.Retroalimentacion);
        Assert.Equal(entrega, dto.EntregaId);
        Assert.Equal("BORRADOR", dto.Estado);
        Assert.NotEmpty(dto.Version);
        Assert.Single(bd.Filas);
    }

    [Fact]
    public async Task Calificar_sin_entrega_una_actividad_que_la_requiere_esta_permitido()
    {
        // El estudiante no entregó y el profesor decide ponerle 0.0.
        var dto = await Caso().EjecutarAsync(Cmd(bd.Taller.Id, Luis, 0.0m), default);
        Assert.Equal(0.0m, dto.Valor);
        Assert.Null(dto.EntregaId);
    }

    // CU-06: nota de una actividad sin entrega (parcial o sustentación)
    [Fact]
    public async Task Registra_la_nota_de_un_parcial_sin_entrega()
    {
        var dto = await Caso().EjecutarAsync(Cmd(bd.Parcial.Id, Ana, 3.0m), default);

        Assert.Equal(3.0m, dto.Valor);
        Assert.Null(dto.EntregaId);
        Assert.Equal("BORRADOR", dto.Estado);
    }

    [Fact]
    public async Task Un_parcial_no_acepta_entregaId()
    {
        var ex = await Assert.ThrowsAsync<ValidacionFallidaException>(() =>
            Caso().EjecutarAsync(Cmd(bd.Parcial.Id, Ana, 3.0m, entrega: Guid.NewGuid()), default));
        Assert.Equal("VALIDACION_FALLIDA", ex.Codigo);
        Assert.Empty(bd.Filas);
    }

    [Fact]
    public async Task Un_cero_se_guarda_como_nota_y_es_distinto_de_sin_calificar()
    {
        await Caso().EjecutarAsync(Cmd(bd.Parcial.Id, Ana, 0.0m), default);

        Assert.Single(bd.Filas);                         // Ana tiene fila: calificada con 0
        Assert.Equal(0.0m, bd.Filas[0].Valor);
        Assert.Null(await bd.ObtenerAsync(bd.Parcial.Id, Luis, default)); // Luis no: sin calificar
    }

    [Fact]
    public async Task Sin_valor_se_rechaza_en_lugar_de_asumir_cero()
    {
        var ex = await Assert.ThrowsAsync<ValidacionFallidaException>(() =>
            Caso().EjecutarAsync(Cmd(bd.Parcial.Id, Ana, null), default));
        Assert.Equal("VALIDACION_FALLIDA", ex.Codigo);
        Assert.Empty(bd.Filas);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5.5)]
    public async Task Nota_fuera_de_rango_devuelve_NOTA_FUERA_DE_RANGO(double valor)
    {
        var ex = await Assert.ThrowsAsync<NotaFueraDeRangoException>(() =>
            Caso().EjecutarAsync(Cmd(bd.Parcial.Id, Ana, (decimal)valor), default));
        Assert.Equal("NOTA_FUERA_DE_RANGO", ex.Codigo);
        Assert.Empty(bd.Filas);
    }

    // CU-07: modificar una nota ya subida, por ejemplo ante un reclamo
    [Fact]
    public async Task Modificar_una_nota_publicada_actualiza_la_misma_fila_y_vuelve_a_borrador()
    {
        var original = bd.Sembrar(bd.Parcial, Ana, 3.0m, publicada: true);
        var idOriginal = original.Id;

        var dto = await Caso().EjecutarAsync(Cmd(bd.Parcial.Id, Ana, 3.8m, "Reclamo aceptado: punto 3 mal calificado."), default);

        Assert.Single(bd.Filas);                 // no se duplica
        Assert.Equal(idOriginal, dto.Id);
        Assert.Equal(3.8m, dto.Valor);
        Assert.Equal("BORRADOR", dto.Estado);    // hay que publicar de nuevo
    }

    [Fact]
    public async Task Modificar_sin_cambios_deja_publicada_la_nota()
    {
        bd.Sembrar(bd.Parcial, Ana, 3.0m, publicada: true, retro: "ok");
        var dto = await Caso().EjecutarAsync(Cmd(bd.Parcial.Id, Ana, 3.0m, "ok"), default);
        Assert.Equal("PUBLICADA", dto.Estado);
    }

    [Fact]
    public async Task Modificar_con_la_version_vigente_funciona_y_devuelve_una_version_nueva()
    {
        var c = bd.Sembrar(bd.Parcial, Ana, 3.0m, publicada: false);
        var versionLeida = Convert.ToBase64String(c.Version);

        var dto = await Caso().EjecutarAsync(Cmd(bd.Parcial.Id, Ana, 3.5m, version: versionLeida), default);

        Assert.Equal(3.5m, dto.Valor);
        Assert.NotEqual(versionLeida, dto.Version);
    }

    [Fact]
    public async Task Modificar_con_una_version_desactualizada_devuelve_409()
    {
        var c = bd.Sembrar(bd.Parcial, Ana, 3.0m, publicada: false);
        var versionVieja = Convert.ToBase64String(c.Version);
        await Caso().EjecutarAsync(Cmd(bd.Parcial.Id, Ana, 3.2m), default); // otra persona cambió la nota

        var ex = await Assert.ThrowsAsync<ConflictoConcurrenciaException>(() =>
            Caso().EjecutarAsync(Cmd(bd.Parcial.Id, Ana, 3.9m, version: versionVieja), default));

        Assert.Equal("CONFLICTO_CONCURRENCIA", ex.Codigo);
        Assert.Equal(3.2m, bd.Filas[0].Valor);
    }

    [Fact]
    public async Task Si_otro_cambio_se_adelanta_al_guardar_se_propaga_el_conflicto()
    {
        bd.ForzarConflictoAlGuardar = true;
        await Assert.ThrowsAsync<ConflictoConcurrenciaException>(() =>
            Caso().EjecutarAsync(Cmd(bd.Parcial.Id, Ana, 3.0m), default));
    }

    [Fact]
    public async Task Enviar_version_para_una_calificacion_que_no_existe_devuelve_409()
    {
        await Assert.ThrowsAsync<ConflictoConcurrenciaException>(() =>
            Caso().EjecutarAsync(Cmd(bd.Parcial.Id, Ana, 3.0m, version: "AAAAAAAAB9E="), default));
    }

    // Permisos y pertenencia (RN-15, RN-16)
    [Fact]
    public async Task Un_profesor_ajeno_al_curso_recibe_SIN_PERMISO()
    {
        var otro = new Actor(OtroProfesorId, Roles.Profesor);
        var ex = await Assert.ThrowsAsync<SinPermisoException>(() =>
            Caso().EjecutarAsync(Cmd(bd.Parcial.Id, Ana, 3.0m, actor: otro), default));
        Assert.Equal("SIN_PERMISO", ex.Codigo);
        Assert.Empty(bd.Filas);
    }

    [Fact]
    public async Task Un_estudiante_no_puede_calificar()
    {
        var estudiante = new Actor(Ana, Roles.Estudiante);
        await Assert.ThrowsAsync<SinPermisoException>(() =>
            Caso().EjecutarAsync(Cmd(bd.Parcial.Id, Ana, 5.0m, actor: estudiante), default));
        Assert.Empty(bd.Filas);
    }

    [Fact]
    public async Task Una_actividad_inexistente_devuelve_NO_ENCONTRADO()
    {
        var ex = await Assert.ThrowsAsync<NoEncontradoException>(() =>
            Caso().EjecutarAsync(Cmd(Guid.NewGuid(), Ana, 3.0m), default));
        Assert.Equal("NO_ENCONTRADO", ex.Codigo);
    }

    [Fact]
    public async Task Un_estudiante_no_inscrito_devuelve_NO_ENCONTRADO()
    {
        await Assert.ThrowsAsync<NoEncontradoException>(() =>
            Caso().EjecutarAsync(Cmd(bd.Parcial.Id, Intruso, 3.0m), default));
        Assert.Empty(bd.Filas);
    }
}
