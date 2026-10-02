using Evaluaciones.Application.Abstracciones;
using Evaluaciones.Domain.Calificaciones;
using Evaluaciones.Domain.Cursos;
using Evaluaciones.Domain;

namespace Evaluaciones.UnitTests;

/// <summary>Repositorios en memoria que imitan lo que hace EF Core: la versión cambia en cada guardado.</summary>
public sealed class BaseEnMemoria : IActividadRepository, ICalificacionRepository, IUnidadDeTrabajo
{
    public static readonly Guid ProfesorId = Guid.Parse("a0000000-0000-0000-0000-000000000001");
    public static readonly Guid OtroProfesorId = Guid.Parse("a0000000-0000-0000-0000-000000000002");
    public static readonly Guid Ana = Guid.Parse("b0000000-0000-0000-0000-000000000001");
    public static readonly Guid Luis = Guid.Parse("b0000000-0000-0000-0000-000000000002");
    public static readonly Guid Marta = Guid.Parse("b0000000-0000-0000-0000-000000000003");
    public static readonly Guid Intruso = Guid.Parse("b0000000-0000-0000-0000-000000000099");
    public static readonly Guid CursoId = Guid.Parse("c0000000-0000-0000-0000-000000000001");

    public Actividad Taller = new(Guid.Parse("d0000000-0000-0000-0000-000000000001"), CursoId, "Taller 1", 1, 20, DateTime.UtcNow.AddDays(7), true);
    public Actividad Parcial = new(Guid.Parse("d0000000-0000-0000-0000-000000000002"), CursoId, "Parcial 1", 1, 80, null, false);
    public Actividad Ajena = new(Guid.Parse("d0000000-0000-0000-0000-000000000009"), Guid.Parse("c0000000-0000-0000-0000-000000000002"), "Ajena", 1, 100, null, false);

    private readonly HashSet<Guid> inscritos = [Ana, Luis, Marta];
    private readonly List<Calificacion> filas = [];
    private readonly List<Calificacion> pendientes = [];
    private long contador;

    /// <summary>Simula que otro usuario guardó entre la lectura y el guardado.</summary>
    public bool ForzarConflictoAlGuardar { get; set; }
    public int Guardados { get; private set; }

    public IReadOnlyList<Calificacion> Filas => filas;

    public Task<ContextoActividad?> ObtenerContextoAsync(Guid actividadId, CancellationToken ct)
    {
        if (actividadId == Taller.Id) return Task.FromResult<ContextoActividad?>(new(Taller, ProfesorId));
        if (actividadId == Parcial.Id) return Task.FromResult<ContextoActividad?>(new(Parcial, ProfesorId));
        if (actividadId == Ajena.Id) return Task.FromResult<ContextoActividad?>(new(Ajena, OtroProfesorId));
        return Task.FromResult<ContextoActividad?>(null);
    }

    public Task<bool> EstudianteInscritoAsync(Guid cursoId, Guid estudianteId, CancellationToken ct) =>
        Task.FromResult(cursoId == CursoId && inscritos.Contains(estudianteId));

    public Task<Calificacion?> ObtenerAsync(Guid actividadId, Guid estudianteId, CancellationToken ct) =>
        Task.FromResult(filas.FirstOrDefault(c => c.ActividadId == actividadId && c.EstudianteId == estudianteId));

    public Task<IReadOnlyList<Calificacion>> ListarPorActividadAsync(Guid actividadId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Calificacion>>(filas.Where(c => c.ActividadId == actividadId).ToList());

    public Task<IReadOnlyList<Calificacion>> ListarBorradoresAsync(Guid actividadId, IReadOnlyCollection<Guid>? estudianteIds, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Calificacion>>(filas
            .Where(c => c.ActividadId == actividadId && c.Estado == EstadoCalificacion.Borrador)
            .Where(c => estudianteIds is null || estudianteIds.Contains(c.EstudianteId))
            .ToList());

    public void Agregar(Calificacion calificacion) => pendientes.Add(calificacion);

    public Task GuardarAsync(CancellationToken ct)
    {
        if (ForzarConflictoAlGuardar) throw new ConflictoConcurrenciaException();
        foreach (var c in pendientes) filas.Add(c);
        pendientes.Clear();
        // Como ROWVERSION, solo cambian las filas que se modificaron; para el modelo basta con renovar todas las tocadas.
        foreach (var c in filas) c.Version = BitConverter.GetBytes(++contador);
        Guardados++;
        return Task.CompletedTask;
    }

    /// <summary>Atajo para preparar una calificación ya guardada.</summary>
    public Calificacion Sembrar(Actividad actividad, Guid estudianteId, decimal valor, bool publicada, string? retro = null, Guid? entregaId = null)
    {
        var c = Calificacion.Crear(Guid.NewGuid(), actividad, estudianteId, entregaId, valor, retro);
        if (publicada) c.Publicar();
        filas.Add(c);
        c.Version = BitConverter.GetBytes(++contador);
        return c;
    }
}
