using Asistente.Domain;
using Microsoft.EntityFrameworkCore;

namespace Asistente.Infrastructure.Persistencia;

/// <summary>asistente_db: solo acciones pendientes y auditoría. No guarda notas publicadas ni conversaciones.</summary>
public sealed class AsistenteDbContext(DbContextOptions<AsistenteDbContext> options) : DbContext(options)
{
    public DbSet<AccionPendiente> Acciones => Set<AccionPendiente>();
    public DbSet<RegistroAuditoria> Auditoria => Set<RegistroAuditoria>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<AccionPendiente>(e =>
        {
            e.ToTable("AccionPendiente");
            e.HasKey(a => a.Id);
            e.Property(a => a.Tipo).HasMaxLength(30).IsRequired();
            e.Property(a => a.ActividadTitulo).HasMaxLength(200).IsRequired();
            e.Property(a => a.EstudianteNombre).HasMaxLength(200).IsRequired();
            e.Property(a => a.Valor).HasPrecision(3, 1);
            e.Property(a => a.ValorAnterior).HasPrecision(3, 1);
            e.Property(a => a.Retroalimentacion).HasMaxLength(2000);
            e.Property(a => a.VersionEsperada).HasMaxLength(64);
            e.Property(a => a.Estado).HasConversion<string>().HasMaxLength(20);
            e.Ignore(a => a.Resumen);
            e.HasIndex(a => new { a.CreadaPor, a.Estado });
        });
        b.Entity<RegistroAuditoria>(e =>
        {
            e.ToTable("Auditoria");
            e.HasKey(a => a.Id);
            e.Property(a => a.Rol).HasMaxLength(20).IsRequired();
            e.Property(a => a.Accion).HasMaxLength(50).IsRequired();
            e.Property(a => a.DatosAntes).HasMaxLength(1000);
            e.Property(a => a.DatosDespues).HasMaxLength(1000);
            e.Property(a => a.Resultado).HasMaxLength(100).IsRequired();
            e.Property(a => a.Origen).HasMaxLength(20).IsRequired();
            e.HasIndex(a => new { a.UsuarioId, a.FechaUtc });
        });
    }
}

/// <summary>Solo para `dotnet ef migrations add`: no abre conexión.</summary>
internal sealed class FabricaDisenoAsistente : Microsoft.EntityFrameworkCore.Design.IDesignTimeDbContextFactory<AsistenteDbContext>
{
    public AsistenteDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<AsistenteDbContext>()
            .UseSqlServer(Environment.GetEnvironmentVariable("ConnectionStrings__Default") ?? "Server=diseno;Database=asistente_db").Options);
}
