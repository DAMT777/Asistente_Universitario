using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Unillanos.Entregas.Domain;

namespace Unillanos.Entregas.Infrastructure.Persistencia;

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
        b.Property(e => e.NombreArchivo).HasMaxLength(255).IsRequired();
        b.Property(e => e.Tamano).IsRequired();
        b.Property(e => e.RutaBlob).HasMaxLength(500).IsRequired();

        // RN-07: máximo una entrega por actividad y estudiante.
        b.HasIndex(e => new { e.ActividadId, e.EstudianteId }).IsUnique().HasDatabaseName("UX_Entrega_Actividad_Estudiante");
        b.HasIndex(e => e.EstudianteId).HasDatabaseName("IX_Entrega_Estudiante");
        b.ToTable(t => t.HasCheckConstraint("CK_Entrega_Estado", "[Estado] IN ('ENVIADA', 'ANULADA')"));
    }
}
