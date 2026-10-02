using Evaluaciones.Application.CasosDeUso;
using Evaluaciones.Application.Dtos;
using Evaluaciones.Domain;
using Evaluaciones.Domain.Errores;
using Evaluaciones.UnitTests.Apoyo;

namespace Evaluaciones.UnitTests;

/// <summary>CU-02 (pesos de los cortes), CU-03 (crear y editar actividades) y el endpoint interno. E-10 y E-11.</summary>
public class GestionCursoTests
{
    private static Task<CursoResumenDto> Pesos(Escenario e, decimal? c1, decimal? c2, decimal? c3, Guid? profesorId = null) =>
        new DefinirPesosCortes(e.Base, e.Base).EjecutarAsync(
            profesorId ?? Escenario.ProfesorId, Escenario.CursoId, new SolicitudPesosCortes(c1, c2, c3), default);

    private static GestionarActividad Gestion(Escenario e) => new(e.Base, e.Base, e.Base, e.Reloj);

    private static SolicitudActividad Solicitud(
        string? titulo = "Taller 2", int? corte = 3, decimal? peso = 30, int? diasParaVencer = 10, bool? requiereEntrega = true) =>
        new(titulo, corte, peso,
            diasParaVencer is null ? null : Escenario.Ahora.AddDays(diasParaVencer.Value),
            requiereEntrega);

    // ───────── CU-02 ─────────

    [Fact]
    public async Task CU02_define_los_pesos_de_los_tres_cortes()
    {
        var e = new Escenario();

        var curso = await Pesos(e, 20, 35.5m, 44.5m);

        Assert.Equal(20m, curso.PesoCorte1);
        Assert.Equal(35.5m, curso.PesoCorte2);
        Assert.Equal(44.5m, e.Base.Cursos.Single().PesoCorte3);
        Assert.Equal(1, e.Base.GuardadosRealizados);
    }

    [Fact]
    public async Task CU02_cambiar_los_pesos_cambia_la_definitiva_calculada()
    {
        var e = new Escenario();
        await Pesos(e, 50, 25, 25);

        var ana = await new ObtenerMatrizNotas(e.Base, e.Base).EjecutarAsync(Escenario.Ana, default);

        Assert.Equal(1.6m, ana.Cursos.Single().DefinitivaParcial); // 3.2 × 50 / 100
    }

    [Theory]
    [InlineData(30, 30, 30)]   // E-11: suman 90
    [InlineData(50, 50, 10)]   // suman 110
    [InlineData(-10, 60, 50)]  // negativo
    [InlineData(33.333, 33.333, 33.334)] // más de dos decimales
    public async Task CU02_pesos_invalidos_responden_PESOS_CORTE_INVALIDOS(double c1, double c2, double c3)
    {
        var e = new Escenario();

        var ex = await Assert.ThrowsAsync<DominioException>(() => Pesos(e, (decimal)c1, (decimal)c2, (decimal)c3));

        Assert.Equal(CodigosError.PesosCorteInvalidos, ex.Codigo);
        Assert.Equal(30m, e.Base.Cursos.Single().PesoCorte1); // no cambió nada
        Assert.Equal(0, e.Base.GuardadosRealizados);
    }

    [Fact]
    public async Task CU02_falta_un_peso_responde_VALIDACION_FALLIDA()
    {
        var e = new Escenario();

        var ex = await Assert.ThrowsAsync<DominioException>(() => Pesos(e, 50, 50, null));

        Assert.Equal(CodigosError.ValidacionFallida, ex.Codigo);
    }

    [Fact]
    public async Task CU02_otro_profesor_recibe_SIN_PERMISO()
    {
        var e = new Escenario();

        var ex = await Assert.ThrowsAsync<DominioException>(() => Pesos(e, 30, 30, 40, Escenario.OtroProfesorId));

        Assert.Equal(CodigosError.SinPermiso, ex.Codigo);
    }

    // ───────── CU-03: crear ─────────

    [Fact]
    public async Task CU03_crea_una_actividad_con_entrega_y_fecha_en_utc()
    {
        var e = new Escenario();

        var dto = await Gestion(e).CrearAsync(Escenario.ProfesorId, Escenario.CursoId, Solicitud(titulo: "  Taller 2  "), default);

        var guardada = e.Base.Actividades.Single(a => a.Id == dto.Id);
        Assert.Equal("Taller 2", guardada.Titulo);
        Assert.Equal(3, guardada.Corte);
        Assert.Equal(30m, guardada.Peso);
        Assert.True(guardada.RequiereEntrega);
        Assert.Equal(Escenario.Ahora.UtcDateTime.AddDays(10), guardada.FechaLimite);
        Assert.Equal(DateTimeKind.Utc, guardada.FechaLimite!.Value.Kind);
        Assert.False(dto.Vencida);
        Assert.Equal(1, e.Base.GuardadosRealizados);
    }

    [Fact]
    public async Task CU03_la_fecha_con_zona_horaria_de_Colombia_se_guarda_en_utc()
    {
        var e = new Escenario();
        var colombia = new DateTimeOffset(2026, 10, 15, 23, 59, 0, TimeSpan.FromHours(-5));

        var dto = await Gestion(e).CrearAsync(Escenario.ProfesorId, Escenario.CursoId,
            new SolicitudActividad("Taller 3", 3, 20, colombia, true), default);

        Assert.Equal(new DateTime(2026, 10, 16, 4, 59, 0, DateTimeKind.Utc), dto.FechaLimite);
    }

    [Fact]
    public async Task CU03_crea_un_parcial_sin_entrega_y_sin_fecha()
    {
        var e = new Escenario();

        var dto = await Gestion(e).CrearAsync(Escenario.ProfesorId, Escenario.CursoId,
            Solicitud(titulo: "Sustentación", corte: 3, peso: 40, diasParaVencer: null, requiereEntrega: false), default);

        Assert.Null(dto.FechaLimite);
        Assert.False(dto.RequiereEntrega);
    }

    [Fact]
    public async Task CU03_E10_un_peso_que_pasa_el_corte_de_100_responde_PESOS_ACTIVIDAD_EXCEDIDOS()
    {
        // El corte 1 ya tiene Taller 1 (20) y Parcial 1 (80).
        var e = new Escenario();

        var ex = await Assert.ThrowsAsync<DominioException>(() =>
            Gestion(e).CrearAsync(Escenario.ProfesorId, Escenario.CursoId, Solicitud(corte: 1, peso: 5), default));

        Assert.Equal(CodigosError.PesosActividadExcedidos, ex.Codigo);
        Assert.Equal(3, e.Base.Actividades.Count);
    }

    [Fact]
    public async Task CU03_un_corte_puede_quedar_en_exactamente_100()
    {
        var e = new Escenario();
        e.Base.Actividades.RemoveAll(a => a.Id == Escenario.Proyecto1);

        var dto = await Gestion(e).CrearAsync(Escenario.ProfesorId, Escenario.CursoId, Solicitud(corte: 2, peso: 100), default);

        Assert.Equal(100m, dto.Peso);
    }

    [Theory]
    [InlineData("", 2, 30)]     // sin título
    [InlineData("Taller", 4, 30)] // corte inválido
    [InlineData("Taller", 2, 0)]  // peso 0 (RN-02: mayor que 0)
    [InlineData("Taller", 2, 101)]
    [InlineData("Taller", 2, 10.555)]
    public async Task CU03_datos_invalidos_responden_VALIDACION_FALLIDA(string titulo, int corte, double peso)
    {
        var e = new Escenario();

        var ex = await Assert.ThrowsAsync<DominioException>(() =>
            Gestion(e).CrearAsync(Escenario.ProfesorId, Escenario.CursoId, Solicitud(titulo, corte, (decimal)peso), default));

        Assert.Equal(CodigosError.ValidacionFallida, ex.Codigo);
    }

    [Fact]
    public async Task CU03_con_entrega_y_sin_fecha_limite_responde_VALIDACION_FALLIDA()
    {
        var e = new Escenario();

        var ex = await Assert.ThrowsAsync<DominioException>(() =>
            Gestion(e).CrearAsync(Escenario.ProfesorId, Escenario.CursoId, Solicitud(diasParaVencer: null), default));

        Assert.Equal(CodigosError.ValidacionFallida, ex.Codigo);
    }

    [Fact]
    public async Task CU03_una_fecha_limite_pasada_responde_VALIDACION_FALLIDA()
    {
        var e = new Escenario();

        var ex = await Assert.ThrowsAsync<DominioException>(() =>
            Gestion(e).CrearAsync(Escenario.ProfesorId, Escenario.CursoId, Solicitud(diasParaVencer: -1), default));

        Assert.Equal(CodigosError.ValidacionFallida, ex.Codigo);
    }

    [Fact]
    public async Task CU03_otro_profesor_no_crea_actividades_en_un_curso_ajeno()
    {
        var e = new Escenario();

        var ex = await Assert.ThrowsAsync<DominioException>(() =>
            Gestion(e).CrearAsync(Escenario.OtroProfesorId, Escenario.CursoId, Solicitud(), default));

        Assert.Equal(CodigosError.SinPermiso, ex.Codigo);
    }

    // ───────── CU-03: editar ─────────

    [Fact]
    public async Task CU03_editar_el_peso_no_se_cuenta_a_si_misma()
    {
        // Parcial 1 pasa de 80 a 80: el corte sigue en 100 y no debe rechazarse.
        var e = new Escenario();

        var dto = await Gestion(e).EditarAsync(Escenario.ProfesorId, Escenario.Parcial1,
            Solicitud(titulo: "Parcial 1 (presencial)", corte: 1, peso: 80, diasParaVencer: null, requiereEntrega: false), default);

        Assert.Equal("Parcial 1 (presencial)", dto.Titulo);
        Assert.Equal(1, e.Base.GuardadosRealizados);
    }

    [Fact]
    public async Task CU03_editar_subiendo_el_peso_por_encima_de_100_se_rechaza()
    {
        var e = new Escenario();

        var ex = await Assert.ThrowsAsync<DominioException>(() =>
            Gestion(e).EditarAsync(Escenario.ProfesorId, Escenario.Taller1, Solicitud(corte: 1, peso: 25, diasParaVencer: 7), default));

        Assert.Equal(CodigosError.PesosActividadExcedidos, ex.Codigo);
        Assert.Equal(20m, e.Base.Actividades.Single(a => a.Id == Escenario.Taller1).Peso);
    }

    [Fact]
    public async Task CU03_mover_una_actividad_de_corte_valida_el_corte_de_destino()
    {
        var e = new Escenario();

        var dto = await Gestion(e).EditarAsync(Escenario.ProfesorId, Escenario.Taller1, Solicitud(corte: 3, peso: 20, diasParaVencer: 7), default);

        Assert.Equal(3, dto.Corte);
        var ex = await Assert.ThrowsAsync<DominioException>(() =>
            Gestion(e).EditarAsync(Escenario.ProfesorId, Escenario.Parcial1, Solicitud(corte: 2, peso: 80, diasParaVencer: null, requiereEntrega: false), default));
        Assert.Equal(CodigosError.PesosActividadExcedidos, ex.Codigo); // el corte 2 ya tiene Proyecto 1 con 100
    }

    [Fact]
    public async Task CU03_conservar_una_fecha_limite_ya_vencida_al_editar_es_valido()
    {
        // Proyecto 1 venció hace 3 días. Se le cambia el título sin tocar la fecha.
        var e = new Escenario();
        var proyecto = e.Base.Actividades.Single(a => a.Id == Escenario.Proyecto1);
        var fecha = new DateTimeOffset(proyecto.FechaLimite!.Value);

        var dto = await Gestion(e).EditarAsync(Escenario.ProfesorId, Escenario.Proyecto1,
            new SolicitudActividad("Proyecto final", 2, 100, fecha, true), default);

        Assert.Equal("Proyecto final", dto.Titulo);
        Assert.True(dto.Vencida);
    }

    [Fact]
    public async Task CU03_la_fecha_vencida_que_llega_sin_segundos_desde_el_formulario_se_considera_la_misma()
    {
        var e = new Escenario();
        var proyecto = e.Base.Actividades.Single(a => a.Id == Escenario.Proyecto1);
        proyecto.FechaLimite = proyecto.FechaLimite!.Value.AddSeconds(37);
        var sinSegundos = new DateTimeOffset(proyecto.FechaLimite.Value.AddSeconds(-proyecto.FechaLimite.Value.Second));

        var dto = await Gestion(e).EditarAsync(Escenario.ProfesorId, Escenario.Proyecto1,
            new SolicitudActividad("Proyecto 1", 2, 90, sinSegundos, true), default);

        Assert.Equal(90m, dto.Peso);
    }

    [Fact]
    public async Task CU03_cambiar_la_fecha_a_otra_ya_pasada_se_rechaza()
    {
        var e = new Escenario();

        var ex = await Assert.ThrowsAsync<DominioException>(() => Gestion(e).EditarAsync(Escenario.ProfesorId, Escenario.Taller1,
            new SolicitudActividad("Taller 1", 1, 20, Escenario.Ahora.AddDays(-1), true), default));

        Assert.Equal(CodigosError.ValidacionFallida, ex.Codigo);
    }

    [Fact]
    public async Task CU03_editar_una_actividad_inexistente_responde_NO_ENCONTRADO()
    {
        var e = new Escenario();

        var ex = await Assert.ThrowsAsync<DominioException>(() =>
            Gestion(e).EditarAsync(Escenario.ProfesorId, Guid.NewGuid(), Solicitud(), default));

        Assert.Equal(CodigosError.NoEncontrado, ex.Codigo);
    }

    // ───────── Detalle del curso y endpoint interno ─────────

    [Fact]
    public async Task Detalle_del_curso_para_el_profesor_y_para_un_estudiante_inscrito()
    {
        var e = new Escenario();
        var obtener = new ObtenerCurso(e.Base, e.Base, e.Base, e.Base);

        var delProfesor = await obtener.EjecutarAsync(Escenario.ProfesorId, Roles.Profesor, Escenario.CursoId, default);
        var deAna = await obtener.EjecutarAsync(Escenario.Ana, Roles.Estudiante, Escenario.CursoId, default);
        var ex = await Assert.ThrowsAsync<DominioException>(() =>
            obtener.EjecutarAsync(Guid.NewGuid(), Roles.Estudiante, Escenario.CursoId, default));

        Assert.Null(delProfesor.ActividadesPendientes);
        Assert.Equal(1, deAna.ActividadesPendientes);
        Assert.Equal(CodigosError.NoEncontrado, ex.Codigo);
    }

    [Fact]
    public async Task Interno_devuelve_el_profesor_la_fecha_y_si_el_estudiante_esta_inscrito()
    {
        var e = new Escenario();
        var consultar = new ObtenerActividadInterna(e.Base, e.Base, e.Base);

        var conAna = await consultar.EjecutarAsync(Escenario.Taller1, Escenario.Ana, default);
        var conExtrano = await consultar.EjecutarAsync(Escenario.Taller1, Guid.NewGuid(), default);
        var sinEstudiante = await consultar.EjecutarAsync(Escenario.Parcial1, null, default);

        Assert.Equal(Escenario.ProfesorId, conAna.ProfesorId);
        Assert.Equal(Escenario.CursoId, conAna.CursoId);
        Assert.True(conAna.RequiereEntrega);
        Assert.NotNull(conAna.FechaLimite);
        Assert.True(conAna.EstudianteInscrito);
        Assert.False(conExtrano.EstudianteInscrito);
        Assert.False(sinEstudiante.EstudianteInscrito); // sin estudianteId no hay inscripción que verificar
        Assert.False(sinEstudiante.RequiereEntrega);
    }
}
