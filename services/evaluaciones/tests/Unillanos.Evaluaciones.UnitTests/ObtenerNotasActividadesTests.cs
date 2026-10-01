using Unillanos.Evaluaciones.Application.CasosDeUso;
using Unillanos.Evaluaciones.Application.Puertos;
using Unillanos.Evaluaciones.Domain;
using Unillanos.Evaluaciones.Domain.Entidades;

namespace Unillanos.Evaluaciones.UnitTests;

public sealed class ObtenerNotasActividadesTests
{
    private static readonly Guid CursoId = Guid.NewGuid();
    private static readonly Guid Estudiante = Guid.NewGuid();
    private static readonly Actividad Taller = new()
    {
        Id = Guid.NewGuid(), CursoId = CursoId, Titulo = "Taller", Corte = 1, Peso = 20,
        FechaLimite = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc), RequiereEntrega = true,
    };

    [Fact]
    public async Task Un_borrador_nunca_se_expone_aunque_el_repositorio_lo_devuelva()
    {
        var borrador = Calificacion(EstadoCalificacion.BORRADOR, 2.5m);
        var caso = Crear(inscrito: true, [borrador]);

        var r = await caso.EjecutarAsync(CursoId, Estudiante, CancellationToken.None);

        var fila = Assert.Single(r.Actividades);
        Assert.Equal(EstadosNotaEstudiante.SinCalificar, fila.Estado);
        Assert.Null(fila.Nota);
        Assert.Null(fila.Retroalimentacion);
    }

    [Fact]
    public async Task Una_calificacion_publicada_se_muestra_con_su_retroalimentacion()
    {
        var caso = Crear(inscrito: true, [Calificacion(EstadoCalificacion.PUBLICADA, 4.5m)]);

        var fila = Assert.Single((await caso.EjecutarAsync(CursoId, Estudiante, CancellationToken.None)).Actividades);

        Assert.Equal(EstadosNotaEstudiante.Publicada, fila.Estado);
        Assert.Equal(4.5m, fila.Nota);
        Assert.Equal("ok", fila.Retroalimentacion);
    }

    [Fact]
    public async Task Sin_inscripcion_responde_sin_permiso()
        => await Assert.ThrowsAsync<SinPermisoException>(() => Crear(inscrito: false, []).EjecutarAsync(CursoId, Estudiante, CancellationToken.None));

    [Fact]
    public async Task Curso_inexistente_responde_no_encontrado()
        => await Assert.ThrowsAsync<NoEncontradoException>(() => Crear(inscrito: true, []).EjecutarAsync(Guid.NewGuid(), Estudiante, CancellationToken.None));

    private static Calificacion Calificacion(EstadoCalificacion estado, decimal valor) => new()
    {
        Id = Guid.NewGuid(), ActividadId = Taller.Id, EstudianteId = Estudiante, Valor = valor,
        Retroalimentacion = "ok", Estado = estado, Version = 1,
    };

    private static ObtenerNotasActividades Crear(bool inscrito, IReadOnlyList<Calificacion> calificaciones)
        => new(new VerificadorInscripcion(new Cursos(inscrito)), new Actividades(), new Calificaciones(calificaciones));

    private sealed class Cursos(bool inscrito) : ICursoRepositorio
    {
        public Task<Curso?> ObtenerAsync(Guid cursoId, CancellationToken ct) => Task.FromResult(cursoId == CursoId
            ? new Curso { Id = CursoId, Codigo = "1", Nombre = "C", ProfesorId = Guid.NewGuid(), ProfesorNombre = "P", PesoCorte1 = 30, PesoCorte2 = 30, PesoCorte3 = 40 }
            : null);
        public Task<bool> EstaInscritoAsync(Guid cursoId, Guid estudianteId, CancellationToken ct) => Task.FromResult(inscrito);
        public Task<IReadOnlyList<Curso>> ListarDeEstudianteAsync(Guid estudianteId, CancellationToken ct) => throw new NotSupportedException();
    }

    private sealed class Actividades : IActividadRepositorio
    {
        public Task<Actividad?> ObtenerAsync(Guid actividadId, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<Actividad>> ListarDeCursoAsync(Guid cursoId, CancellationToken ct) => Task.FromResult<IReadOnlyList<Actividad>>([Taller]);
    }

    private sealed class Calificaciones(IReadOnlyList<Calificacion> datos) : ICalificacionRepositorio
    {
        public Task<IReadOnlyList<Calificacion>> ListarPublicadasAsync(Guid estudianteId, IReadOnlyCollection<Guid> actividadIds, CancellationToken ct) => Task.FromResult(datos);
    }
}
