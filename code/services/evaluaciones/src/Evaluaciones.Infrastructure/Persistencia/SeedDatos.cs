using Evaluaciones.Domain.Calificaciones;
using Evaluaciones.Domain.Cursos;
using Microsoft.EntityFrameworkCore;

namespace Evaluaciones.Infrastructure.Persistencia;

/// <summary>
/// Datos semilla de desarrollo (guía 9.8). Identificadores fijos y proceso idempotente.
/// Solo se ejecuta en el entorno Development.
/// </summary>
public static class SeedDatos
{
    public static readonly Guid ProfesorId = Guid.Parse("a0000000-0000-0000-0000-000000000001");
    public static readonly Guid Ana = Guid.Parse("b0000000-0000-0000-0000-000000000001");
    public static readonly Guid Luis = Guid.Parse("b0000000-0000-0000-0000-000000000002");
    public static readonly Guid Marta = Guid.Parse("b0000000-0000-0000-0000-000000000003");
    public static readonly Guid CursoId = Guid.Parse("c0000000-0000-0000-0000-000000000001");
    public static readonly Guid Taller1 = Guid.Parse("d0000000-0000-0000-0000-000000000001");
    public static readonly Guid Parcial1 = Guid.Parse("d0000000-0000-0000-0000-000000000002");
    public static readonly Guid Proyecto1 = Guid.Parse("d0000000-0000-0000-0000-000000000003");

    public static async Task AplicarAsync(EvaluacionesDbContext db, TimeProvider reloj, CancellationToken ct = default)
    {
        var ahora = reloj.GetUtcNow().UtcDateTime;

        if (!await db.Cursos.AnyAsync(c => c.Id == CursoId, ct))
            db.Cursos.Add(new Curso(CursoId, "603803", "Simulación computacional", ProfesorId, "Profesor Demo", 30, 30, 40));

        var estudiantes = new[]
        {
            new CursoEstudiante(CursoId, Ana, "Ana Demo", "E0001"),
            new CursoEstudiante(CursoId, Luis, "Luis Demo", "E0002"),
            new CursoEstudiante(CursoId, Marta, "Marta Demo", "E0003"),
        };
        foreach (var e in estudiantes)
            if (!await db.CursoEstudiantes.AnyAsync(x => x.CursoId == e.CursoId && x.EstudianteId == e.EstudianteId, ct))
                db.CursoEstudiantes.Add(e);

        var taller = new Actividad(Taller1, CursoId, "Taller 1", 1, 20, ahora.AddDays(7), requiereEntrega: true);
        var parcial = new Actividad(Parcial1, CursoId, "Parcial 1", 1, 80, null, requiereEntrega: false);
        var proyecto = new Actividad(Proyecto1, CursoId, "Proyecto 1", 2, 100, ahora.AddDays(-3), requiereEntrega: true);
        foreach (var a in new[] { taller, parcial, proyecto })
            if (!await db.Actividades.AnyAsync(x => x.Id == a.Id, ct))
                db.Actividades.Add(a);

        await db.SaveChangesAsync(ct);

        // Ana: Taller 4.0 y Parcial 3.0 publicadas. Luis: Taller 2.5 en borrador. Marta: sin calificaciones.
        await SembrarCalificacionAsync(db, taller, Ana, 4.0m, publicada: true, ct);
        await SembrarCalificacionAsync(db, parcial, Ana, 3.0m, publicada: true, ct);
        await SembrarCalificacionAsync(db, taller, Luis, 2.5m, publicada: false, ct);
        await db.SaveChangesAsync(ct);
    }

    private static async Task SembrarCalificacionAsync(EvaluacionesDbContext db, Actividad actividad, Guid estudianteId, decimal valor, bool publicada, CancellationToken ct)
    {
        if (await db.Calificaciones.AnyAsync(c => c.ActividadId == actividad.Id && c.EstudianteId == estudianteId, ct))
            return;

        var c = Calificacion.Crear(Guid.NewGuid(), actividad, estudianteId, entregaId: null, valor, retroalimentacion: null);
        if (publicada) c.Publicar();
        db.Calificaciones.Add(c);
    }
}
