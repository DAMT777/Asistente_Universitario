using Asistente.Application;
using Asistente.Application.CasosDeUso;
using Asistente.Domain;

namespace Asistente.UnitTests;

public sealed class AccionesPendientesTests
{
    private static readonly Guid Profesor = Guid.NewGuid();
    private readonly RelojManual _reloj = new(new DateTimeOffset(2026, 11, 14, 12, 0, 0, TimeSpan.Zero));
    private readonly AccionesEnMemoria _acciones = new();
    private readonly GatewayEscrituras _gateway = new();
    private readonly GestionarAcciones _casoDeUso;

    public AccionesPendientesTests() => _casoDeUso = new(_acciones, _gateway, new AuditoriaEnMemoria(), _reloj);

    private AccionPendiente Nueva()
    {
        var a = AccionPendiente.Proponer(Profesor, Guid.NewGuid(), Guid.NewGuid(), "Taller 1", Guid.NewGuid(), "Luis Demo",
            4.2m, null, 2.5m, null, _reloj.GetUtcNow(), TimeSpan.FromMinutes(5));
        _acciones.Filas[a.Id] = a;
        return a;
    }

    private static UsuarioAsistente ProfesorActual => new(Profesor, UsuarioAsistente.Profesor);

    [Fact]
    public async Task Se_usa_una_sola_vez()
    {
        var a = Nueva();
        await _casoDeUso.ConfirmarAsync(ProfesorActual, a.Id, default);
        var e = await Assert.ThrowsAsync<ExcepcionAsistente>(() => _casoDeUso.ConfirmarAsync(ProfesorActual, a.Id, default));
        Assert.Equal(CodigosAsistente.AccionYaUsada, e.Codigo);
        Assert.Equal(1, _gateway.Escrituras);
    }

    [Fact]
    public async Task Expira_a_los_cinco_minutos()
    {
        var a = Nueva();
        _reloj.Ahora = _reloj.Ahora.AddMinutes(5);
        var e = await Assert.ThrowsAsync<ExcepcionAsistente>(() => _casoDeUso.ConfirmarAsync(ProfesorActual, a.Id, default));
        Assert.Equal(CodigosAsistente.AccionExpirada, e.Codigo);
        Assert.Equal(410, e.Status);
        Assert.Equal(0, _gateway.Escrituras);
    }

    [Fact]
    public async Task Antes_de_expirar_si_se_puede_confirmar()
    {
        var a = Nueva();
        _reloj.Ahora = _reloj.Ahora.AddMinutes(4).AddSeconds(59);
        await _casoDeUso.ConfirmarAsync(ProfesorActual, a.Id, default);
        Assert.Equal(EstadoAccion.Confirmada, _acciones.Filas[a.Id].Estado);
    }

    [Fact]
    public async Task Otro_usuario_no_la_encuentra()
    {
        var a = Nueva();
        var e = await Assert.ThrowsAsync<ExcepcionAsistente>(() =>
            _casoDeUso.ConfirmarAsync(new(Guid.NewGuid(), UsuarioAsistente.Profesor), a.Id, default));
        Assert.Equal(CodigosAsistente.AccionNoEncontrada, e.Codigo);
        Assert.Equal(404, e.Status);
    }

    [Fact]
    public async Task Cancelada_no_se_puede_confirmar()
    {
        var a = Nueva();
        await _casoDeUso.CancelarAsync(ProfesorActual, a.Id, default);
        var e = await Assert.ThrowsAsync<ExcepcionAsistente>(() => _casoDeUso.ConfirmarAsync(ProfesorActual, a.Id, default));
        Assert.Equal(CodigosAsistente.AccionYaUsada, e.Codigo);
        Assert.Equal(0, _gateway.Escrituras);
    }

    [Theory]
    [InlineData(5.1)]
    [InlineData(-0.1)]
    [InlineData(4.25)]
    public void Nota_invalida_no_crea_accion(double valor) =>
        Assert.Throws<ExcepcionAsistente>(() => AccionPendiente.Proponer(Profesor, Guid.NewGuid(), Guid.NewGuid(), "T", Guid.NewGuid(), "E",
            (decimal)valor, null, null, null, DateTimeOffset.UtcNow, TimeSpan.FromMinutes(5)));
}
