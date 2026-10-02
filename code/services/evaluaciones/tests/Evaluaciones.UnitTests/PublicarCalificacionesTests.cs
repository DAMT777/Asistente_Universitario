using Evaluaciones.Application;
using Evaluaciones.Application.Calificaciones;
using Evaluaciones.Domain;
using Evaluaciones.Domain.Calificaciones;
using Xunit;
using static Evaluaciones.UnitTests.BaseEnMemoria;

namespace Evaluaciones.UnitTests;

public class PublicarCalificacionesTests
{
    private readonly BaseEnMemoria bd = new();
    private static readonly Actor Profesor = new(ProfesorId, Roles.Profesor);

    private PublicarCalificaciones Caso() => new(bd, bd, bd);

    // CU-08: publicar las calificaciones de una actividad
    [Fact]
    public async Task Publica_todos_los_borradores_de_la_actividad()
    {
        bd.Sembrar(bd.Taller, Ana, 4.0m, publicada: false);
        bd.Sembrar(bd.Taller, Luis, 2.5m, publicada: false);

        var r = await Caso().EjecutarAsync(new(Profesor, bd.Taller.Id, null), default);

        Assert.Equal(2, r.Publicadas);
        Assert.All(bd.Filas, c => Assert.Equal(EstadoCalificacion.Publicada, c.Estado));
        Assert.Equal(1, bd.Guardados);
    }

    [Fact]
    public async Task No_toca_las_calificaciones_de_otras_actividades()
    {
        bd.Sembrar(bd.Taller, Ana, 4.0m, publicada: false);
        var parcial = bd.Sembrar(bd.Parcial, Ana, 3.0m, publicada: false);

        await Caso().EjecutarAsync(new(Profesor, bd.Taller.Id, null), default);

        Assert.Equal(EstadoCalificacion.Borrador, parcial.Estado);
    }

    [Fact]
    public async Task Puede_publicar_solo_algunos_estudiantes()
    {
        var ana = bd.Sembrar(bd.Taller, Ana, 4.0m, publicada: false);
        var luis = bd.Sembrar(bd.Taller, Luis, 2.5m, publicada: false);

        var r = await Caso().EjecutarAsync(new(Profesor, bd.Taller.Id, [Ana]), default);

        Assert.Equal(1, r.Publicadas);
        Assert.Equal(EstadoCalificacion.Publicada, ana.Estado);
        Assert.Equal(EstadoCalificacion.Borrador, luis.Estado);
    }

    [Fact]
    public async Task Sin_borradores_devuelve_cero_y_no_guarda()
    {
        bd.Sembrar(bd.Taller, Ana, 4.0m, publicada: true);

        var r = await Caso().EjecutarAsync(new(Profesor, bd.Taller.Id, null), default);

        Assert.Equal(0, r.Publicadas);
        Assert.Equal(0, bd.Guardados);
    }

    [Fact]
    public async Task Un_estudiante_sin_calificar_no_aparece_ni_se_publica()
    {
        bd.Sembrar(bd.Taller, Ana, 4.0m, publicada: false);

        await Caso().EjecutarAsync(new(Profesor, bd.Taller.Id, null), default);

        Assert.Single(bd.Filas); // Luis y Marta siguen sin fila
    }

    [Fact]
    public async Task Despues_de_modificar_una_nota_publicada_hay_que_publicar_de_nuevo()
    {
        var c = bd.Sembrar(bd.Parcial, Ana, 3.0m, publicada: true);
        await new GuardarCalificacion(bd, bd, bd).EjecutarAsync(new(Profesor, bd.Parcial.Id, Ana, 3.8m, "Reclamo", null, null), default);
        Assert.Equal(EstadoCalificacion.Borrador, c.Estado);

        var r = await Caso().EjecutarAsync(new(Profesor, bd.Parcial.Id, null), default);

        Assert.Equal(1, r.Publicadas);
        Assert.Equal(EstadoCalificacion.Publicada, c.Estado);
        Assert.Equal(3.8m, c.Valor);
    }

    [Fact]
    public async Task Un_profesor_ajeno_no_puede_publicar()
    {
        await Assert.ThrowsAsync<SinPermisoException>(() =>
            Caso().EjecutarAsync(new(new Actor(OtroProfesorId, Roles.Profesor), bd.Parcial.Id, null), default));
    }

    [Fact]
    public async Task Un_estudiante_no_puede_publicar()
    {
        await Assert.ThrowsAsync<SinPermisoException>(() =>
            Caso().EjecutarAsync(new(new Actor(Ana, Roles.Estudiante), bd.Parcial.Id, null), default));
    }

    [Fact]
    public async Task Publicar_una_actividad_inexistente_devuelve_NO_ENCONTRADO()
    {
        await Assert.ThrowsAsync<NoEncontradoException>(() =>
            Caso().EjecutarAsync(new(Profesor, Guid.NewGuid(), null), default));
    }
}

public class ListarCalificacionesTests
{
    private readonly BaseEnMemoria bd = new();

    [Fact]
    public async Task El_profesor_dueno_ve_borradores_y_publicadas_pero_no_a_quien_esta_sin_calificar()
    {
        bd.Sembrar(bd.Taller, Ana, 4.0m, publicada: true);
        bd.Sembrar(bd.Taller, Luis, 2.5m, publicada: false);

        var lista = await new ListarCalificaciones(bd, bd).EjecutarAsync(new(ProfesorId, Roles.Profesor), bd.Taller.Id, default);

        Assert.Equal(2, lista.Count);
        Assert.Contains(lista, c => c.EstudianteId == Luis && c.Estado == "BORRADOR");
        Assert.DoesNotContain(lista, c => c.EstudianteId == Marta);
    }

    [Fact]
    public async Task Un_estudiante_no_puede_listar_calificaciones_de_la_actividad()
    {
        await Assert.ThrowsAsync<SinPermisoException>(() =>
            new ListarCalificaciones(bd, bd).EjecutarAsync(new(Ana, Roles.Estudiante), bd.Taller.Id, default));
    }
}
