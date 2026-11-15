using Asistente.Application;
using Asistente.Application.Herramientas;

namespace Asistente.UnitTests;

public sealed class SeleccionHerramientasTests
{
    private static readonly IHerramienta[] Todas =
    [
        new HerramientaFalsa("consultar_mis_notas", UsuarioAsistente.Estudiante, false),
        new HerramientaFalsa("consultar_mis_entregas", UsuarioAsistente.Estudiante, false),
        new HerramientaFalsa("escritura_mal_registrada", UsuarioAsistente.Estudiante, true),
        new HerramientaFalsa("listar_estudiantes_curso", UsuarioAsistente.Profesor, false),
        new HerramientaFalsa("proponer_calificacion", UsuarioAsistente.Profesor, true),
    ];

    [Fact]
    public void El_estudiante_recibe_solo_lecturas_de_su_rol()
    {
        var nombres = new CatalogoHerramientas(Todas).Para(new(Guid.NewGuid(), UsuarioAsistente.Estudiante)).Select(h => h.Nombre);
        Assert.Equal(["consultar_mis_notas", "consultar_mis_entregas"], nombres);
    }

    [Fact]
    public void El_estudiante_nunca_recibe_escritura_aunque_este_registrada_con_su_rol()
    {
        var herramientas = new CatalogoHerramientas(Todas).Para(new(Guid.NewGuid(), UsuarioAsistente.Estudiante));
        Assert.DoesNotContain(herramientas, h => h.EsEscritura);
        Assert.DoesNotContain(herramientas, h => h.Nombre == "proponer_calificacion");
    }

    [Fact]
    public void El_profesor_recibe_las_suyas()
    {
        var nombres = new CatalogoHerramientas(Todas).Para(new(Guid.NewGuid(), UsuarioAsistente.Profesor)).Select(h => h.Nombre);
        Assert.Equal(["listar_estudiantes_curso", "proponer_calificacion"], nombres);
    }

    [Fact]
    public void Un_rol_desconocido_no_recibe_nada() =>
        Assert.Empty(new CatalogoHerramientas(Todas).Para(new(Guid.NewGuid(), "ADMIN")));
}
