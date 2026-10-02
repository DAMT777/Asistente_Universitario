using Evaluaciones.Application.CasosDeUso;
using Evaluaciones.Domain.Entidades;
using Evaluaciones.Domain.Errores;
using Evaluaciones.UnitTests.Apoyo;

namespace Evaluaciones.UnitTests;

/// <summary>Lecturas de apoyo que el profesor necesita para CU-09, CU-10 y CU-11.</summary>
public class LecturasDelProfesorTests
{
    [Fact]
    public async Task Lista_los_estudiantes_inscritos_ordenados_por_nombre()
    {
        var e = new Escenario();

        var lista = await new ListarEstudiantesDelCurso(e.Base, e.Base)
            .EjecutarAsync(Escenario.ProfesorId, Escenario.CursoId, default);

        Assert.Equal(new[] { "Ana Demo", "Luis Demo", "Marta Demo" }, lista.Select(s => s.Nombre));
        Assert.All(lista, s => Assert.Equal("ESTUDIANTE", s.Rol));
        Assert.Equal("E0001", lista[0].Codigo);
    }

    [Fact]
    public async Task Otro_profesor_no_puede_listar_los_estudiantes()
    {
        var e = new Escenario();

        var ex = await Assert.ThrowsAsync<DominioException>(() => new ListarEstudiantesDelCurso(e.Base, e.Base)
            .EjecutarAsync(Escenario.OtroProfesorId, Escenario.CursoId, default));

        Assert.Equal(CodigosError.SinPermiso, ex.Codigo);
    }

    [Fact]
    public async Task Lista_las_calificaciones_de_la_actividad_incluidos_los_borradores()
    {
        var e = new Escenario();

        var lista = await new ListarCalificacionesDeActividad(e.Base, e.Base, e.Base)
            .EjecutarAsync(Escenario.ProfesorId, Escenario.Taller1, default);

        Assert.Equal(2, lista.Count); // Ana (publicada) y Luis (borrador); Marta está sin calificar
        Assert.Equal("PUBLICADA", lista.Single(c => c.EstudianteId == Escenario.Ana).Estado);
        Assert.Equal("BORRADOR", lista.Single(c => c.EstudianteId == Escenario.Luis).Estado);
        Assert.DoesNotContain(lista, c => c.EstudianteId == Escenario.Marta);
    }

    [Fact]
    public async Task Una_calificacion_en_cero_aparece_como_calificada()
    {
        var e = new Escenario();
        e.Calificar(Escenario.Marta, Escenario.Taller1, 0.0m, EstadoCalificacion.Publicada);

        var lista = await new ListarCalificacionesDeActividad(e.Base, e.Base, e.Base)
            .EjecutarAsync(Escenario.ProfesorId, Escenario.Taller1, default);

        Assert.Equal(0.0m, lista.Single(c => c.EstudianteId == Escenario.Marta).Valor);
    }

    [Fact]
    public async Task Actividad_inexistente_recibe_NO_ENCONTRADO()
    {
        var e = new Escenario();

        var ex = await Assert.ThrowsAsync<DominioException>(() => new ListarCalificacionesDeActividad(e.Base, e.Base, e.Base)
            .EjecutarAsync(Escenario.ProfesorId, Guid.NewGuid(), default));

        Assert.Equal(CodigosError.NoEncontrado, ex.Codigo);
    }

    [Fact]
    public async Task Otro_profesor_no_puede_ver_las_calificaciones_de_la_actividad()
    {
        var e = new Escenario();

        var ex = await Assert.ThrowsAsync<DominioException>(() => new ListarCalificacionesDeActividad(e.Base, e.Base, e.Base)
            .EjecutarAsync(Escenario.OtroProfesorId, Escenario.Taller1, default));

        Assert.Equal(CodigosError.SinPermiso, ex.Codigo);
    }

    [Fact]
    public async Task El_ponderado_del_curso_incluye_las_publicaciones_de_corte()
    {
        var e = new Escenario();

        var curso = await new ConsultarPonderado(e.Base, e.Base, e.Base, e.Base, e.Base)
            .PorCursoAsync(Escenario.ProfesorId, Escenario.CursoId, default);

        var publicacion = Assert.Single(curso.Publicaciones);
        Assert.Equal(Escenario.Ana, publicacion.EstudianteId);
        Assert.Equal(1, publicacion.Corte);
        Assert.Equal(3.2m, publicacion.Nota);
    }

    [Fact]
    public async Task La_lista_de_cursos_trae_los_pesos_de_los_cortes()
    {
        var e = new Escenario();

        var cursos = await new ListarCursos(e.Base, e.Base, e.Base)
            .EjecutarAsync(Escenario.ProfesorId, Evaluaciones.Domain.Roles.Profesor, default);

        var curso = Assert.Single(cursos);
        Assert.Equal(Escenario.CursoId, curso.Id);
        Assert.Equal((30m, 30m, 40m), (curso.PesoCorte1, curso.PesoCorte2, curso.PesoCorte3));
    }
}
