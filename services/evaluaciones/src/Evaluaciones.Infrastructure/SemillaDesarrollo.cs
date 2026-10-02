using Evaluaciones.Domain.Entidades;
using Evaluaciones.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Evaluaciones.Infrastructure;

/// <summary>
/// Datos semilla de la sección 9.8 de la guía técnica. Idempotente: si ya hay cursos no hace nada.
/// Los identificadores son fijos porque los comparten los tres servicios.
/// </summary>
public static class SemillaDesarrollo
{
    public static readonly Guid ProfesorId = new("a0000000-0000-0000-0000-000000000001");
    public static readonly Guid Ana = new("b0000000-0000-0000-0000-000000000001");
    public static readonly Guid Luis = new("b0000000-0000-0000-0000-000000000002");
    public static readonly Guid Marta = new("b0000000-0000-0000-0000-000000000003");
    public static readonly Guid CursoId = new("c0000000-0000-0000-0000-000000000001");
    public static readonly Guid Taller1 = new("d0000000-0000-0000-0000-000000000001");
    public static readonly Guid Parcial1 = new("d0000000-0000-0000-0000-000000000002");
    public static readonly Guid Proyecto1 = new("d0000000-0000-0000-0000-000000000003");

    // Entregas de demostración del servicio de entregas (mismos identificadores en los dos servicios).
    public static readonly Guid EntregaTallerAna = new("e0000000-0000-0000-0000-000000000001");
    public static readonly Guid EntregaTallerLuis = new("e0000000-0000-0000-0000-000000000002");

    public static async Task AplicarAsync(IServiceProvider servicios, TimeProvider reloj, CancellationToken ct = default)
    {
        using var alcance = servicios.CreateScope();
        var db = alcance.ServiceProvider.GetRequiredService<EvaluacionesDbContext>();

        // Solo la base en memoria se crea sola. Con SQL Server el esquema viene de las migraciones.
        if (db.Database.IsInMemory())
            await db.Database.EnsureCreatedAsync(ct);

        if (await db.Cursos.AnyAsync(ct)) return;

        var ahora = reloj.GetUtcNow().UtcDateTime;

        db.Cursos.Add(new Curso
        {
            Id = CursoId, Codigo = "603803", Nombre = "Simulación computacional",
            ProfesorId = ProfesorId, ProfesorNombre = "Profesor Demo",
            PesoCorte1 = 30, PesoCorte2 = 30, PesoCorte3 = 40
        });

        db.Inscripciones.AddRange(
            new CursoEstudiante { CursoId = CursoId, EstudianteId = Ana, EstudianteNombre = "Ana Demo", EstudianteCodigo = "E0001" },
            new CursoEstudiante { CursoId = CursoId, EstudianteId = Luis, EstudianteNombre = "Luis Demo", EstudianteCodigo = "E0002" },
            new CursoEstudiante { CursoId = CursoId, EstudianteId = Marta, EstudianteNombre = "Marta Demo", EstudianteCodigo = "E0003" });

        db.Actividades.AddRange(
            new Actividad { Id = Taller1, CursoId = CursoId, Titulo = "Taller 1", Corte = 1, Peso = 20, FechaLimite = ahora.AddDays(7), RequiereEntrega = true },
            new Actividad { Id = Parcial1, CursoId = CursoId, Titulo = "Parcial 1", Corte = 1, Peso = 80, FechaLimite = null, RequiereEntrega = false },
            new Actividad { Id = Proyecto1, CursoId = CursoId, Titulo = "Proyecto 1", Corte = 2, Peso = 100, FechaLimite = ahora.AddDays(-3), RequiereEntrega = true });

        // Ana: ambas publicadas y corte 1 ya publicado con 3.2. Luis: borrador (prueba RN-12). Marta: sin calificaciones.
        db.Calificaciones.AddRange(
            new Calificacion { Id = Guid.NewGuid(), ActividadId = Taller1, EstudianteId = Ana, EntregaId = EntregaTallerAna, Valor = 4.0m, Retroalimentacion = "Buen trabajo.", Estado = EstadoCalificacion.Publicada },
            new Calificacion { Id = Guid.NewGuid(), ActividadId = Parcial1, EstudianteId = Ana, Valor = 3.0m, Retroalimentacion = "", Estado = EstadoCalificacion.Publicada },
            new Calificacion { Id = Guid.NewGuid(), ActividadId = Taller1, EstudianteId = Luis, EntregaId = EntregaTallerLuis, Valor = 2.5m, Retroalimentacion = "", Estado = EstadoCalificacion.Borrador });

        db.PublicacionesCorte.Add(new PublicacionCorte
        {
            Id = Guid.NewGuid(), CursoId = CursoId, EstudianteId = Ana, Corte = 1, Nota = 3.2m, FechaPublicacion = ahora
        });

        await db.SaveChangesAsync(ct);
    }
}
