using Evaluaciones.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Evaluaciones.Infrastructure.Persistencia;

/// <summary>Esquema de evaluaciones_db (sección 9.3 de la guía técnica).</summary>
public class EvaluacionesDbContext(DbContextOptions<EvaluacionesDbContext> opciones) : DbContext(opciones)
{
    public DbSet<Curso> Cursos => Set<Curso>();
    public DbSet<CursoEstudiante> Inscripciones => Set<CursoEstudiante>();
    public DbSet<Actividad> Actividades => Set<Actividad>();
    public DbSet<Calificacion> Calificaciones => Set<Calificacion>();
    public DbSet<PublicacionCorte> PublicacionesCorte => Set<PublicacionCorte>();

    protected override void OnModelCreating(ModelBuilder modelo)
    {
        modelo.Entity<Curso>(e =>
        {
            e.ToTable("Curso");
            e.HasKey(x => x.Id);
            e.Property(x => x.Codigo).HasMaxLength(20).IsUnicode(false);
            e.Property(x => x.Nombre).HasMaxLength(150);
            e.Property(x => x.ProfesorNombre).HasMaxLength(150);
            e.Property(x => x.PesoCorte1).HasPrecision(5, 2);
            e.Property(x => x.PesoCorte2).HasPrecision(5, 2);
            e.Property(x => x.PesoCorte3).HasPrecision(5, 2);
        });

        modelo.Entity<CursoEstudiante>(e =>
        {
            e.ToTable("CursoEstudiante");
            e.HasKey(x => new { x.CursoId, x.EstudianteId });
            e.Property(x => x.EstudianteNombre).HasMaxLength(150);
            e.Property(x => x.EstudianteCodigo).HasMaxLength(20).IsUnicode(false);
            e.HasOne<Curso>().WithMany().HasForeignKey(x => x.CursoId);
            e.HasIndex(x => x.EstudianteId);
        });

        modelo.Entity<Actividad>(e =>
        {
            e.ToTable("Actividad");
            e.HasKey(x => x.Id);
            e.Property(x => x.Titulo).HasMaxLength(150);
            e.Property(x => x.Peso).HasPrecision(5, 2);
            e.Property(x => x.FechaLimite).HasColumnType("datetime2").HasConversion(EnUtc.Nulable);
            e.HasOne<Curso>().WithMany().HasForeignKey(x => x.CursoId);
            e.HasIndex(x => new { x.CursoId, x.Corte });
        });

        modelo.Entity<Calificacion>(e =>
        {
            e.ToTable("Calificacion");
            e.HasKey(x => x.Id);
            e.Property(x => x.Valor).HasPrecision(3, 2);
            e.Property(x => x.Retroalimentacion).HasMaxLength(2000);
            e.Property(x => x.Estado)
                .HasColumnType("varchar(10)")
                .HasConversion(
                    v => v == EstadoCalificacion.Borrador ? "BORRADOR" : "PUBLICADA",
                    v => v == "BORRADOR" ? EstadoCalificacion.Borrador : EstadoCalificacion.Publicada);
            e.Property(x => x.Version).IsRowVersion();
            e.HasOne<Actividad>().WithMany().HasForeignKey(x => x.ActividadId);
            e.HasIndex(x => new { x.ActividadId, x.EstudianteId }).IsUnique();
            e.HasIndex(x => x.EstudianteId);
        });

        modelo.Entity<PublicacionCorte>(e =>
        {
            e.ToTable("PublicacionCorte");
            e.HasKey(x => x.Id);
            e.Property(x => x.Nota).HasPrecision(3, 2);
            e.Property(x => x.FechaPublicacion).HasColumnType("datetime2").HasConversion(EnUtc.Valor);
            e.Property(x => x.Version).IsRowVersion();
            e.HasOne<Curso>().WithMany().HasForeignKey(x => x.CursoId);
            e.HasIndex(x => new { x.CursoId, x.EstudianteId, x.Corte }).IsUnique();
        });
    }
}
