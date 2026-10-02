using Evaluaciones.Application.CasosDeUso;
using Evaluaciones.Domain.Entidades;
using Evaluaciones.Domain.Errores;
using Evaluaciones.UnitTests.Apoyo;

namespace Evaluaciones.UnitTests;

/// <summary>CU-16 (matriz) y CU-15 (notas por actividad) del rol estudiante. Escenarios E-07, E-13 y E-14.</summary>
public class MisNotasTests
{
    private static ObtenerMatrizNotas Matriz(Escenario e) => new(e.Base, e.Base);
    private static ObtenerNotasActividades Notas(Escenario e) => new(e.Base, e.Base, e.Base, e.Base);

    [Fact]
    public async Task E13_Ana_corte_1_es_3_2_y_definitiva_parcial_1_0()
    {
        var curso = Assert.Single((await Matriz(new Escenario()).EjecutarAsync(Escenario.Ana, default)).Cursos);

        Assert.Equal(("603803", "Profesor Demo"), (curso.Codigo, curso.Profesor));
        Assert.Equal((1, 30m, (decimal?)3.2m, true), (curso.Cortes[0].Corte, curso.Cortes[0].PesoCorte, curso.Cortes[0].Nota, curso.Cortes[0].Publicado));
        Assert.All(curso.Cortes.Skip(1), c => Assert.Equal(((decimal?)null, false), (c.Nota, c.Publicado)));
        Assert.Equal(1.0m, curso.DefinitivaParcial);
        Assert.True(curso.EsParcial);
    }

    [Fact]
    public async Task E14_Marta_sin_cortes_publicados_tiene_definitiva_0_y_es_parcial()
    {
        var curso = Assert.Single((await Matriz(new Escenario()).EjecutarAsync(Escenario.Marta, default)).Cursos);

        Assert.All(curso.Cortes, c => Assert.Equal(((decimal?)null, false), (c.Nota, c.Publicado)));
        Assert.Equal(0m, curso.DefinitivaParcial);
        Assert.True(curso.EsParcial);
    }

    [Fact]
    public async Task Con_los_tres_cortes_publicados_la_definitiva_ya_no_es_parcial()
    {
        var e = new Escenario();
        e.Publicar(Escenario.Ana, corte: 2, nota: 4.0m);
        e.Publicar(Escenario.Ana, corte: 3, nota: 3.5m);

        var curso = Assert.Single((await Matriz(e).EjecutarAsync(Escenario.Ana, default)).Cursos);

        // 3.2×30 + 4.0×30 + 3.5×40 = 0.96 + 1.2 + 1.4 = 3.56 → 3.6 (DA-01).
        Assert.Equal(3.6m, curso.DefinitivaParcial);
        Assert.False(curso.EsParcial);
    }

    [Fact]
    public async Task Un_estudiante_sin_cursos_recibe_una_matriz_vacia()
        => Assert.Empty((await Matriz(new Escenario()).EjecutarAsync(Guid.NewGuid(), default)).Cursos);

    [Fact]
    public async Task Ana_ve_sus_notas_publicadas_y_lo_no_calificado_como_SIN_CALIFICAR()
    {
        var r = await Notas(new Escenario()).EjecutarAsync(Escenario.Ana, Escenario.CursoId, default);

        var porTitulo = r.Actividades.ToDictionary(a => a.Titulo);
        Assert.Equal(("PUBLICADA", (decimal?)4.0m), (porTitulo["Taller 1"].Estado, porTitulo["Taller 1"].Nota));
        Assert.Equal(("PUBLICADA", (decimal?)3.0m), (porTitulo["Parcial 1"].Estado, porTitulo["Parcial 1"].Nota));
        Assert.Equal(("SIN_CALIFICAR", (decimal?)null), (porTitulo["Proyecto 1"].Estado, porTitulo["Proyecto 1"].Nota));
        Assert.Null(porTitulo["Parcial 1"].FechaLimite);
    }

    [Fact]
    public async Task E07_Luis_no_ve_su_borrador_sino_SIN_CALIFICAR_y_nota_null()
    {
        var e = new Escenario();
        e.CalificacionDe(Escenario.Luis, Escenario.Taller1).Retroalimentacion = "Borrador: pendiente de revisión.";

        var taller = (await Notas(e).EjecutarAsync(Escenario.Luis, Escenario.CursoId, default)).Actividades.Single(a => a.Titulo == "Taller 1");

        Assert.Equal(("SIN_CALIFICAR", (decimal?)null, (string?)null), (taller.Estado, taller.Nota, taller.Retroalimentacion));
    }

    [Fact]
    public async Task Un_estudiante_no_inscrito_recibe_SIN_PERMISO()
    {
        var ex = await Assert.ThrowsAsync<DominioException>(() => Notas(new Escenario()).EjecutarAsync(Guid.NewGuid(), Escenario.CursoId, default));
        Assert.Equal(CodigosError.SinPermiso, ex.Codigo);
    }

    [Fact]
    public async Task Un_curso_inexistente_responde_NO_ENCONTRADO()
    {
        var ex = await Assert.ThrowsAsync<DominioException>(() => Notas(new Escenario()).EjecutarAsync(Escenario.Ana, Guid.NewGuid(), default));
        Assert.Equal(CodigosError.NoEncontrado, ex.Codigo);
    }
}

/// <summary>Consulta interna que usa el servicio de entregas.</summary>
public class ActividadInternaTests
{
    private static ObtenerActividadInterna Caso(Escenario e) => new(e.Base, e.Base, e.Base);

    [Fact]
    public async Task Informa_profesor_fecha_requiere_entrega_e_inscripcion()
    {
        var e = new Escenario();
        var r = await Caso(e).EjecutarAsync(Escenario.Taller1, Escenario.Ana, default);

        Assert.Equal((Escenario.CursoId, Escenario.ProfesorId, true, true), (r.CursoId, r.ProfesorId, r.RequiereEntrega, r.EstudianteInscrito));
        Assert.Equal(e.Base.Actividades.Single(a => a.Id == Escenario.Taller1).FechaLimite, r.FechaLimite);
    }

    [Fact]
    public async Task Sin_estudiante_o_con_uno_no_inscrito_EstudianteInscrito_es_false()
    {
        var e = new Escenario();
        Assert.False((await Caso(e).EjecutarAsync(Escenario.Taller1, null, default)).EstudianteInscrito);
        Assert.False((await Caso(e).EjecutarAsync(Escenario.Taller1, Guid.NewGuid(), default)).EstudianteInscrito);
    }

    [Fact]
    public async Task Actividad_inexistente_responde_NO_ENCONTRADO()
    {
        var ex = await Assert.ThrowsAsync<DominioException>(() => Caso(new Escenario()).EjecutarAsync(Guid.NewGuid(), Escenario.Ana, default));
        Assert.Equal(CodigosError.NoEncontrado, ex.Codigo);
    }
}
