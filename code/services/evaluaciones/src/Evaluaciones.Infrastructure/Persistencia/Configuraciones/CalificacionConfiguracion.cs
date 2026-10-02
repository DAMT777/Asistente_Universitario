using Evaluaciones.Domain.Calificaciones;
using Evaluaciones.Domain.Cursos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Evaluaciones.Infrastructure.Persistencia.Configuraciones;

internal sealed class CalificacionConfiguracion : IEntityTypeConfiguration<Calificacion>
{
    public void Configure(EntityTypeBuilder<Calificacion> b)
    {
        b.ToTable("Calificacion", t =>
        {
            t.HasCheckConstraint("CK_Calificacion_Valor", "[Valor] BETWEEN 0 AND 5");
            t.HasCheckConstraint("CK_Calificacion_Estado", "[Estado] IN ('BORRADOR','PUBLICADA')");
        });
        b.HasKey(c => c.Id);
        b.Property(c => c.Id).ValueGeneratedNever();

        // EstudianteId y EntregaId referencian a otros servicios: columnas comunes, sin llave foránea.
        b.Property(c => c.EstudianteId).IsRequired();
        b.Property(c => c.EntregaId);

        b.Property(c => c.Valor).HasPrecision(3, 2).IsRequired();
        b.Property(c => c.Retroalimentacion).HasMaxLength(2000);

        b.Property(c => c.Estado)
            .HasConversion(
                v => v == EstadoCalificacion.Publicada ? "PUBLICADA" : "BORRADOR",
                v => v == "PUBLICADA" ? EstadoCalificacion.Publicada : EstadoCalificacion.Borrador)
            .HasMaxLength(10)
            .IsUnicode(false)
            .IsRequired();

        // Control de concurrencia (guía 9.6): si la fila cambió desde que se leyó, SaveChanges falla.
        b.Property(c => c.Version).IsRowVersion();

        b.HasOne<Actividad>().WithMany().HasForeignKey(c => c.ActividadId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(c => new { c.ActividadId, c.EstudianteId }).IsUnique();
        b.HasIndex(c => c.EstudianteId);
    }
}
