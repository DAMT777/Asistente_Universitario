using Entregas.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Entregas.Infrastructure.Persistencia;

/// <summary>Esquema de entregas_db (sección 9.4 de la guía técnica).</summary>
public class EntregasDbContext(DbContextOptions<EntregasDbContext> opciones) : DbContext(opciones)
{
    public DbSet<Entrega> Entregas => Set<Entrega>();

    // datetime2 no guarda la zona: todas las fechas son UTC (DA-10) y al leerlas se marcan así.
    private static readonly ValueConverter<DateTime, DateTime> EnUtc =
        new(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

    protected override void OnModelCreating(ModelBuilder modelo)
    {
        modelo.Entity<Entrega>(e =>
        {
            e.ToTable("Entrega");
            e.HasKey(x => x.Id);
            e.Property(x => x.FechaEnvio).HasColumnType("datetime2").HasConversion(EnUtc);
            e.Property(x => x.Estado)
                .HasColumnType("varchar(10)")
                .HasConversion(
                    v => v == EstadoEntrega.Enviada ? "ENVIADA" : "ANULADA",
                    v => v == "ENVIADA" ? EstadoEntrega.Enviada : EstadoEntrega.Anulada);
            e.Property(x => x.NombreArchivo).HasMaxLength(255);
            e.Property(x => x.RutaBlob).HasMaxLength(500);
            e.HasIndex(x => new { x.ActividadId, x.EstudianteId }).IsUnique(); // RN-07
            e.HasIndex(x => x.ActividadId); // sección 9.5: el profesor ve las entregas de una actividad
        });
    }
}
