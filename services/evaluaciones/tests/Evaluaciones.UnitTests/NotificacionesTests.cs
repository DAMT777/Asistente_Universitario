using Evaluaciones.Application.CasosDeUso;
using Evaluaciones.Application.Dtos;
using Evaluaciones.Domain.Entidades;
using Evaluaciones.Domain.Errores;
using Evaluaciones.UnitTests.Apoyo;

namespace Evaluaciones.UnitTests;

/// <summary>
/// Notificaciones del estudiante: se crean junto con el cambio del profesor que las origina
/// (actividad nueva o editada, pesos, nota de actividad o de corte publicada) y el estudiante las consulta y marca.
/// </summary>
public class NotificacionesTests
{
    private static GestionarActividad Gestion(Escenario e) => new(e.Base, e.Base, e.Base, e.Reloj, e.Notificador);
    private static GestionarNotificaciones Bandeja(Escenario e) => new(e.Base, e.Base);

    private static SolicitudActividad Solicitud(string titulo = "Taller 2", int corte = 3, decimal peso = 30, int dias = 10) =>
        new(titulo, corte, peso, Escenario.Ahora.AddDays(dias), true);

    private static List<Notificacion> De(Escenario e, Guid estudianteId) =>
        e.Base.Notificaciones.Where(n => n.UsuarioId == estudianteId).ToList();

    // ───────── Lo que genera notificaciones ─────────

    [Fact]
    public async Task Crear_una_actividad_avisa_a_todos_los_inscritos_en_el_mismo_guardado()
    {
        var e = new Escenario();

        var creada = await Gestion(e).CrearAsync(Escenario.ProfesorId, Escenario.CursoId, Solicitud(), default);

        Assert.Equal(3, e.Base.Notificaciones.Count);
        Assert.All(e.Base.Notificaciones, n =>
        {
            Assert.Equal(TipoNotificacion.NuevaActividad, n.Tipo);
            Assert.Equal(creada.Id, n.ActividadId);
            Assert.Equal(Escenario.CursoId, n.CursoId);
            Assert.False(n.Leida);
            Assert.Contains("Taller 2", n.Titulo);
            Assert.Equal(Escenario.Ahora.UtcDateTime, n.FechaCreacion);
        });
        Assert.Equal(new[] { Escenario.Ana, Escenario.Luis, Escenario.Marta }, e.Base.Notificaciones.Select(n => n.UsuarioId).Order());
        Assert.Equal(1, e.Base.GuardadosRealizados);
    }

    [Fact]
    public async Task La_fecha_limite_del_aviso_va_en_hora_de_Colombia()
    {
        var e = new Escenario();
        var solicitud = new SolicitudActividad("Quiz", 3, 10, new DateTimeOffset(2026, 10, 20, 4, 59, 0, TimeSpan.Zero), true);

        await Gestion(e).CrearAsync(Escenario.ProfesorId, Escenario.CursoId, solicitud, default);

        Assert.Contains("19/10/2026 23:59", De(e, Escenario.Ana).Single().Mensaje);
    }

    [Fact]
    public async Task Una_actividad_rechazada_no_avisa()
    {
        var e = new Escenario();

        await Assert.ThrowsAsync<DominioException>(() =>
            Gestion(e).CrearAsync(Escenario.ProfesorId, Escenario.CursoId, Solicitud(corte: 1, peso: 50), default));

        Assert.Empty(e.Base.Notificaciones);
    }

    [Fact]
    public async Task Editar_una_actividad_avisa_que_cambio_y_dice_que()
    {
        var e = new Escenario();
        var nuevaFecha = Escenario.Ahora.AddDays(12);

        await Gestion(e).EditarAsync(
            Escenario.ProfesorId, Escenario.Taller1, new SolicitudActividad("Taller 1", 1, 20, nuevaFecha, true), default);

        var aviso = De(e, Escenario.Marta).Single();
        Assert.Equal(TipoNotificacion.CursoActualizado, aviso.Tipo);
        Assert.Equal(Escenario.Taller1, aviso.ActividadId);
        Assert.Contains("nueva fecha límite", aviso.Mensaje);
        Assert.Equal(3, e.Base.Notificaciones.Count);
    }

    [Fact]
    public async Task Guardar_una_actividad_sin_cambios_no_avisa()
    {
        var e = new Escenario();
        var taller = e.Base.Actividades.Single(a => a.Id == Escenario.Taller1);

        await Gestion(e).EditarAsync(
            Escenario.ProfesorId, Escenario.Taller1,
            new SolicitudActividad(taller.Titulo, taller.Corte, taller.Peso, new DateTimeOffset(taller.FechaLimite!.Value), true), default);

        Assert.Empty(e.Base.Notificaciones);
    }

    [Fact]
    public async Task Cambiar_los_pesos_de_los_cortes_avisa_y_dejarlos_igual_no()
    {
        var e = new Escenario();
        var pesos = new DefinirPesosCortes(e.Base, e.Base, e.Notificador);

        await pesos.EjecutarAsync(Escenario.ProfesorId, Escenario.CursoId, new SolicitudPesosCortes(30, 30, 40), default);
        Assert.Empty(e.Base.Notificaciones);

        await pesos.EjecutarAsync(Escenario.ProfesorId, Escenario.CursoId, new SolicitudPesosCortes(20, 40, 40), default);
        var aviso = De(e, Escenario.Ana).Single();
        Assert.Equal(TipoNotificacion.CursoActualizado, aviso.Tipo);
        Assert.Null(aviso.ActividadId);
        Assert.Contains("20%, 40% y 40%", aviso.Mensaje);
    }

    [Fact]
    public async Task Publicar_notas_de_actividad_avisa_solo_a_quien_se_le_publico_y_con_su_nota()
    {
        var e = new Escenario();
        e.Calificar(Escenario.Marta, Escenario.Taller1, 3.5m, EstadoCalificacion.Borrador);

        await new PublicarCalificacionesDeActividad(e.Base, e.Base, e.Base, e.Base, e.Notificador)
            .EjecutarAsync(Escenario.ProfesorId, Escenario.Taller1, default);

        Assert.Empty(De(e, Escenario.Ana)); // ya estaba publicada
        Assert.Contains("2.5", De(e, Escenario.Luis).Single().Mensaje);
        var marta = De(e, Escenario.Marta).Single();
        Assert.Equal(TipoNotificacion.NotaPublicada, marta.Tipo);
        Assert.Equal(Escenario.Taller1, marta.ActividadId);
        Assert.Contains("3.5", marta.Mensaje);
    }

    [Fact]
    public async Task Calificar_en_borrador_no_avisa()
    {
        var e = new Escenario();

        await new CalificarActividad(e.Base, e.Base, e.Base, e.Base, e.Base).EjecutarAsync(
            Escenario.ProfesorId, Escenario.Taller1, Escenario.Marta, new SolicitudCalificar(4.0m, "Bien", null), null, default);

        Assert.Empty(e.Base.Notificaciones);
    }

    [Fact]
    public async Task Publicar_un_corte_avisa_a_los_publicados_y_no_a_los_rechazados()
    {
        var e = new Escenario(anaConCorte1Publicado: false);

        await new PublicarCorte(e.Base, e.Base, e.Base, e.Base, e.Base, e.Base, e.Reloj, e.Notificador)
            .EjecutarAsync(Escenario.ProfesorId, Escenario.CursoId, 1, new SolicitudPublicarCorte(), default);

        var ana = De(e, Escenario.Ana).Single();
        Assert.Equal("Nota del corte 1 publicada", ana.Titulo);
        Assert.Contains("3.2", ana.Mensaje);
        Assert.Empty(De(e, Escenario.Luis));
        Assert.Empty(De(e, Escenario.Marta));
    }

    [Fact]
    public async Task Volver_a_publicar_la_misma_nota_de_corte_no_repite_el_aviso()
    {
        var e = new Escenario(); // Ana ya tiene el corte 1 publicado con 3.2

        await new PublicarCorte(e.Base, e.Base, e.Base, e.Base, e.Base, e.Base, e.Reloj, e.Notificador)
            .EjecutarAsync(Escenario.ProfesorId, Escenario.CursoId, 1, new SolicitudPublicarCorte([Escenario.Ana]), default);

        Assert.Empty(e.Base.Notificaciones);
    }

    [Fact]
    public async Task Corregir_un_corte_con_otra_nota_avisa_que_se_corrigio()
    {
        var e = new Escenario();
        e.CalificacionDe(Escenario.Ana, Escenario.Parcial1).Valor = 5.0m;

        await new CorregirCorte(e.Base, e.Base, e.Base, e.Base, e.Base, e.Reloj, e.Notificador).EjecutarAsync(
            Escenario.ProfesorId, Escenario.CursoId, 1, Escenario.Ana, new SolicitudCorregirCorte(), null, default);

        var aviso = De(e, Escenario.Ana).Single();
        Assert.Equal("Nota del corte 1 corregida", aviso.Titulo);
        Assert.Contains("4.8", aviso.Mensaje);
    }

    // ───────── Bandeja del estudiante ─────────

    [Fact]
    public async Task Sin_notificaciones_la_bandeja_esta_vacia()
    {
        var e = new Escenario();

        var bandeja = await Bandeja(e).ListarAsync(Escenario.Ana, default);

        Assert.Equal(0, bandeja.NoLeidas);
        Assert.Empty(bandeja.Notificaciones);
    }

    [Fact]
    public async Task La_bandeja_trae_solo_las_propias_mas_recientes_primero()
    {
        var e = new Escenario();
        Agregar(e, Escenario.Ana, "Vieja", Escenario.Ahora.UtcDateTime.AddHours(-2));
        Agregar(e, Escenario.Ana, "Nueva", Escenario.Ahora.UtcDateTime);
        Agregar(e, Escenario.Luis, "De Luis", Escenario.Ahora.UtcDateTime);

        var bandeja = await Bandeja(e).ListarAsync(Escenario.Ana, default);

        Assert.Equal(new[] { "Nueva", "Vieja" }, bandeja.Notificaciones.Select(n => n.Titulo));
        Assert.Equal(2, bandeja.NoLeidas);
        Assert.Equal("NUEVA_ACTIVIDAD", bandeja.Notificaciones[0].Tipo);
    }

    [Fact]
    public async Task Marcar_leida_una_propia_la_marca_y_una_ajena_responde_NO_ENCONTRADO()
    {
        var e = new Escenario();
        var deAna = Agregar(e, Escenario.Ana, "Hola", Escenario.Ahora.UtcDateTime);

        var ex = await Assert.ThrowsAsync<DominioException>(() => Bandeja(e).MarcarLeidaAsync(Escenario.Luis, deAna.Id, default));
        Assert.Equal(CodigosError.NoEncontrado, ex.Codigo);
        Assert.False(deAna.Leida);

        await Bandeja(e).MarcarLeidaAsync(Escenario.Ana, deAna.Id, default);
        Assert.True(deAna.Leida);
        Assert.Equal(1, e.Base.GuardadosRealizados);

        await Bandeja(e).MarcarLeidaAsync(Escenario.Ana, deAna.Id, default); // repetir no escribe
        Assert.Equal(1, e.Base.GuardadosRealizados);
    }

    [Fact]
    public async Task Marcar_todas_leidas_solo_toca_las_del_usuario()
    {
        var e = new Escenario();
        Agregar(e, Escenario.Ana, "1", Escenario.Ahora.UtcDateTime);
        Agregar(e, Escenario.Ana, "2", Escenario.Ahora.UtcDateTime);
        var deLuis = Agregar(e, Escenario.Luis, "3", Escenario.Ahora.UtcDateTime);

        var resultado = await Bandeja(e).MarcarTodasLeidasAsync(Escenario.Ana, default);

        Assert.Equal(2, resultado.Marcadas);
        Assert.Equal(0, (await Bandeja(e).ListarAsync(Escenario.Ana, default)).NoLeidas);
        Assert.False(deLuis.Leida);
    }

    private static Notificacion Agregar(Escenario e, Guid usuarioId, string titulo, DateTime fecha)
    {
        var n = Notificacion.Nueva(usuarioId, Escenario.CursoId, null, TipoNotificacion.NuevaActividad, titulo, "Mensaje", fecha);
        e.Base.Notificaciones.Add(n);
        return n;
    }
}
