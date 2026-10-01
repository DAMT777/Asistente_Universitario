using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Evaluaciones.Domain.Entidades;

namespace Evaluaciones.Infrastructure.Persistencia;

/// <summary>
/// Datos semilla con GUID fijos. Solo se ejecuta en Development y es idempotente:
/// inserta únicamente lo que falta, por id. Las fechas límite son relativas al momento de la primera carga
/// (Taller 1 abierta, Proyecto 1 vencida).
/// </summary>
public sealed class SemillaDesarrollo(EvaluacionesDbContext db, TimeProvider reloj, ILogger<SemillaDesarrollo> logger)
{
    public static readonly Guid ProfesorId = Guid.Parse("a0000000-0000-0000-0000-000000000001");
    public static readonly Guid Ana = Guid.Parse("b0000000-0000-0000-0000-000000000001");
    public static readonly Guid Luis = Guid.Parse("b0000000-0000-0000-0000-000000000002");
    public static readonly Guid Marta = Guid.Parse("b0000000-0000-0000-0000-000000000003");
    public static readonly Guid CursoId = Guid.Parse("c0000000-0000-0000-0000-000000000001");
    public static readonly Guid Taller1 = Guid.Parse("d0000000-0000-0000-0000-000000000001");
    public static readonly Guid Parcial1 = Guid.Parse("d0000000-0000-0000-0000-000000000002");
    public static readonly Guid Proyecto1 = Guid.Parse("d0000000-0000-0000-0000-000000000003");

    public async Task CargarAsync(CancellationToken ct)
    {
        var hoy = reloj.GetUtcNow().UtcDateTime;
        hoy = new DateTime(hoy.Year, hoy.Month, hoy.Day, 23, 59, 59, DateTimeKind.Utc);

        await AgregarSiFaltaAsync(db.Cursos, c => c.Id == CursoId, () => new Curso
        {
            Id = CursoId, Codigo = "603803", Nombre = "Simulación Computacional",
            ProfesorId = ProfesorId, ProfesorNombre = "Laura Rincón",
            PesoCorte1 = 30, PesoCorte2 = 30, PesoCorte3 = 40,
        }, ct);

        foreach (var estudiante in new[] { Ana, Luis, Marta })
        {
            await AgregarSiFaltaAsync(db.CursoEstudiantes, ce => ce.CursoId == CursoId && ce.EstudianteId == estudiante,
                () => new CursoEstudiante { CursoId = CursoId, EstudianteId = estudiante }, ct);
        }

        await AgregarSiFaltaAsync(db.Actividades, a => a.Id == Taller1, () => new Actividad
        {
            Id = Taller1, CursoId = CursoId, Titulo = "Taller 1", Corte = 1, Peso = 20,
            FechaLimite = hoy.AddDays(30), RequiereEntrega = true,
        }, ct);
        await AgregarSiFaltaAsync(db.Actividades, a => a.Id == Parcial1, () => new Actividad
        {
            Id = Parcial1, CursoId = CursoId, Titulo = "Parcial 1", Corte = 1, Peso = 80,
            FechaLimite = hoy.AddDays(-3), RequiereEntrega = false,
        }, ct);
        await AgregarSiFaltaAsync(db.Actividades, a => a.Id == Proyecto1, () => new Actividad
        {
            Id = Proyecto1, CursoId = CursoId, Titulo = "Proyecto 1", Corte = 2, Peso = 100,
            FechaLimite = hoy.AddDays(-7), RequiereEntrega = true,
        }, ct);

        await AgregarCalificacionAsync("e0000000-0000-0000-0000-000000000001", Taller1, Ana, 4.0m, EstadoCalificacion.PUBLICADA,
            "Buen análisis; faltó justificar la distribución de llegadas.", ct);
        await AgregarCalificacionAsync("e0000000-0000-0000-0000-000000000002", Parcial1, Ana, 3.0m, EstadoCalificacion.PUBLICADA,
            "Revise el cálculo del intervalo de confianza.", ct);
        await AgregarCalificacionAsync("e0000000-0000-0000-0000-000000000003", Taller1, Luis, 2.5m, EstadoCalificacion.BORRADOR,
            "Borrador: pendiente de revisión.", ct);

        // Corte 1 de Ana publicado: 4.0 x 20% + 3.0 x 80% = 3.2.
        await AgregarSiFaltaAsync(db.PublicacionesCorte, p => p.Id == Guid.Parse("f0000000-0000-0000-0000-000000000001"), () => new PublicacionCorte
        {
            Id = Guid.Parse("f0000000-0000-0000-0000-000000000001"), CursoId = CursoId, EstudianteId = Ana,
            Corte = 1, Nota = 3.2m, FechaPublicacion = hoy.AddDays(-1), Version = 1,
        }, ct);

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Semilla de desarrollo de evaluaciones_db verificada");
    }

    private Task AgregarCalificacionAsync(string id, Guid actividadId, Guid estudianteId, decimal valor,
        EstadoCalificacion estado, string retro, CancellationToken ct)
    {
        var guid = Guid.Parse(id);
        return AgregarSiFaltaAsync(db.Calificaciones, c => c.Id == guid, () => new Calificacion
        {
            Id = guid, ActividadId = actividadId, EstudianteId = estudianteId, EntregaId = null,
            Valor = valor, Retroalimentacion = retro, Estado = estado, Version = 1,
        }, ct);
    }

    private static async Task AgregarSiFaltaAsync<T>(DbSet<T> conjunto, System.Linq.Expressions.Expression<Func<T, bool>> existe,
        Func<T> crear, CancellationToken ct) where T : class
    {
        if (!await conjunto.AnyAsync(existe, ct)) conjunto.Add(crear());
    }
}
