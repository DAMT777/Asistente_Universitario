using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Entregas.Domain;

namespace Entregas.Infrastructure.Persistencia;

internal sealed class EntregaConfiguracion : IEntityTypeConfiguration<Entrega>
{
    // DATETIME2 no guarda la zona: se escribe en UTC y se lee marcado como UTC.
    private static readonly ValueConverter<DateTime, DateTime> Utc = new(
        v => v.ToUniversalTime(),
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

    public void Configure(EntityTypeBuilder<Entrega> b)
    {
        b.ToTable("Entrega");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).ValueGeneratedNever();
        b.Property(e => e.ActividadId).IsRequired();
        b.Property(e => e.EstudianteId).IsRequired();
        b.Property(e => e.FechaEnvio).HasColumnType("datetime2").HasConversion(Utc).IsRequired();
        b.Property(e => e.Estado).HasConversion<string>().HasMaxLength(10).IsUnicode(false).IsRequired();
        b.Ignore(e => e.TamanoTotal);
        b.HasMany(e => e.Archivos).WithOne().HasForeignKey(a => a.EntregaId).OnDelete(DeleteBehavior.Cascade);
        b.Navigation(e => e.Archivos).HasField("_archivos").UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();

        // RN-07: máximo una entrega por actividad y estudiante.
        b.HasIndex(e => new { e.ActividadId, e.EstudianteId }).IsUnique().HasDatabaseName("UX_Entrega_Actividad_Estudiante");
        b.HasIndex(e => e.EstudianteId).HasDatabaseName("IX_Entrega_Estudiante");
        b.ToTable(t => t.HasCheckConstraint("CK_Entrega_Estado", "[Estado] IN ('ENVIADA', 'ANULADA')"));
    }
}

/// <summary>Archivos de cada entrega (misma base; la FK es interna a entregas_db).</summary>
internal sealed class ArchivoEntregaConfiguracion : IEntityTypeConfiguration<ArchivoEntrega>
{
    public void Configure(EntityTypeBuilder<ArchivoEntrega> b)
    {
        b.ToTable("ArchivoEntrega");
        b.HasKey(a => a.Id);
        b.Property(a => a.Id).ValueGeneratedNever();
        b.Property(a => a.NombreArchivo).HasMaxLength(255).IsRequired();
        b.Property(a => a.Tamano).IsRequired();
        b.Property(a => a.ContentType).HasMaxLength(100).IsUnicode(false).IsRequired();
        b.Property(a => a.RutaBlob).HasMaxLength(500).IsRequired();
        b.Property(a => a.Orden).IsRequired();
        b.HasIndex(a => new { a.EntregaId, a.Orden }).IsUnique().HasDatabaseName("UX_ArchivoEntrega_Entrega_Orden");
    }
}
