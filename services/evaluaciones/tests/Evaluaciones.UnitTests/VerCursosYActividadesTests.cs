using Evaluaciones.Application.CasosDeUso;
using Evaluaciones.Domain;
using Evaluaciones.Domain.Entidades;
using Evaluaciones.Domain.Errores;
using Evaluaciones.UnitTests.Apoyo;

namespace Evaluaciones.UnitTests;

/// <summary>CU-12. El estudiante ve sus cursos y sus actividades pendientes.</summary>
public class VerCursosYActividadesTests
{
    private static ListarCursos CursosDe(Escenario e) => new(e.Base, e.Base, e.Base);

    private static ListarActividadesDelCurso ActividadesDe(Escenario e) =>
        new(e.Base, e.Base, e.Base, e.Base, e.Reloj);

    [Fact]
    public async Task Estudiante_ve_su_curso_con_el_conteo_de_pendientes()
    {
        var e = new Escenario();

        var cursos = await CursosDe(e).EjecutarAsync(Escenario.Ana, Roles.Estudiante, default);

        var curso = Assert.Single(cursos);
        Assert.Equal("603803", curso.Codigo);
        Assert.Equal("Profesor Demo", curso.Profesor);
        Assert.Equal(1, curso.ActividadesPendientes); // Taller y Parcial calificados, falta el Proyecto
    }

    [Fact]
    public async Task Un_borrador_no_cuenta_como_calificada_para_el_estudiante()
    {
        var e = new Escenario();

        var cursos = await CursosDe(e).EjecutarAsync(Escenario.Luis, Roles.Estudiante, default);

        Assert.Equal(3, Assert.Single(cursos).ActividadesPendientes);
    }

    [Fact]
    public async Task Estudiante_sin_calificaciones_tiene_todas_pendientes()
    {
        var e = new Escenario();

        var cursos = await CursosDe(e).EjecutarAsync(Escenario.Marta, Roles.Estudiante, default);

        Assert.Equal(3, Assert.Single(cursos).ActividadesPendientes);
    }

    [Fact]
    public async Task Estudiante_no_ve_cursos_donde_no_esta_inscrito()
    {
        var e = new Escenario();

        var cursos = await CursosDe(e).EjecutarAsync(Guid.NewGuid(), Roles.Estudiante, default);

        Assert.Empty(cursos);
    }

    [Fact]
    public async Task Profesor_ve_sus_cursos_sin_conteo_de_pendientes()
    {
        var e = new Escenario();

        var cursos = await CursosDe(e).EjecutarAsync(Escenario.ProfesorId, Roles.Profesor, default);

        Assert.Null(Assert.Single(cursos).ActividadesPendientes);
    }

    [Fact]
    public async Task Profesor_no_ve_cursos_de_otro_profesor()
    {
        var e = new Escenario();

        var cursos = await CursosDe(e).EjecutarAsync(Escenario.OtroProfesorId, Roles.Profesor, default);

        Assert.Empty(cursos);
    }

    [Fact]
    public async Task Rol_desconocido_recibe_SIN_PERMISO()
    {
        var e = new Escenario();

        var ex = await Assert.ThrowsAsync<DominioException>(
            () => CursosDe(e).EjecutarAsync(Escenario.Ana, "ADMIN", default));

        Assert.Equal(CodigosError.SinPermiso, ex.Codigo);
    }

    [Fact]
    public async Task Actividades_del_estudiante_muestran_estado_y_si_estan_vencidas()
    {
        var e = new Escenario();

        var actividades = await ActividadesDe(e).EjecutarAsync(
            Escenario.Ana, Roles.Estudiante, Escenario.CursoId, soloPendientes: false, default);

        Assert.Equal(new[] { "Taller 1", "Parcial 1", "Proyecto 1" }, actividades.Select(a => a.Titulo));

        var taller = actividades.Single(a => a.Id == Escenario.Taller1);
        Assert.Equal("CALIFICADA", taller.Estado);
        Assert.False(taller.Vencida);

        var parcial = actividades.Single(a => a.Id == Escenario.Parcial1);
        Assert.Equal("CALIFICADA", parcial.Estado);
        Assert.False(parcial.Vencida); // sin fecha límite nunca vence

        var proyecto = actividades.Single(a => a.Id == Escenario.Proyecto1);
        Assert.Equal("PENDIENTE", proyecto.Estado);
        Assert.True(proyecto.Vencida);
    }

    [Fact]
    public async Task Filtro_solo_pendientes_devuelve_unicamente_las_sin_calificar()
    {
        var e = new Escenario();

        var actividades = await ActividadesDe(e).EjecutarAsync(
            Escenario.Ana, Roles.Estudiante, Escenario.CursoId, soloPendientes: true, default);

        Assert.Equal(Escenario.Proyecto1, Assert.Single(actividades).Id);
    }

    [Fact]
    public async Task El_estudiante_no_ve_la_nota_de_otro_ni_borradores()
    {
        var e = new Escenario();

        var actividades = await ActividadesDe(e).EjecutarAsync(
            Escenario.Luis, Roles.Estudiante, Escenario.CursoId, soloPendientes: false, default);

        // El Taller de Luis está en borrador, así que para él sigue pendiente.
        Assert.Equal("PENDIENTE", actividades.Single(a => a.Id == Escenario.Taller1).Estado);
    }

    [Fact]
    public async Task Estudiante_no_inscrito_recibe_NO_ENCONTRADO()
    {
        // RN-16
        var e = new Escenario();

        var ex = await Assert.ThrowsAsync<DominioException>(() => ActividadesDe(e).EjecutarAsync(
            Guid.NewGuid(), Roles.Estudiante, Escenario.CursoId, false, default));

        Assert.Equal(CodigosError.NoEncontrado, ex.Codigo);
    }

    [Fact]
    public async Task Profesor_ve_las_actividades_de_su_curso_sin_estado_por_estudiante()
    {
        var e = new Escenario();

        var actividades = await ActividadesDe(e).EjecutarAsync(
            Escenario.ProfesorId, Roles.Profesor, Escenario.CursoId, soloPendientes: true, default);

        Assert.Equal(3, actividades.Count); // el filtro no aplica al profesor
        Assert.All(actividades, a => Assert.Null(a.Estado));
    }

    [Fact]
    public async Task Otro_profesor_recibe_SIN_PERMISO()
    {
        var e = new Escenario();

        var ex = await Assert.ThrowsAsync<DominioException>(() => ActividadesDe(e).EjecutarAsync(
            Escenario.OtroProfesorId, Roles.Profesor, Escenario.CursoId, false, default));

        Assert.Equal(CodigosError.SinPermiso, ex.Codigo);
    }
}
