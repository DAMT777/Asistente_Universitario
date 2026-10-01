using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Evaluaciones.Domain.Entidades;

namespace Evaluaciones.Infrastructure.Persistencia.Configuraciones;

// Las llaves foráneas son solo internas a evaluaciones_db. ProfesorId, EstudianteId y EntregaId
// son referencias a otros servicios y se guardan únicamente como columna de id.

internal sealed class CursoConfiguracion : IEntityTypeConfiguration<Curso>
{
    public void Configure(EntityTypeBuilder<Curso> b)
    {
        b.ToTable("Curso", t => t.HasCheckConstraint("CK_Curso_Pesos", "[PesoCorte1] >= 0 AND [PesoCorte2] >= 0 AND [PesoCorte3] >= 0"));
        b.HasKey(c => c.Id);
        b.Property(c => c.Id).ValueGeneratedNever();
        b.Property(c => c.Codigo).HasMaxLength(20).IsRequired();
        b.Property(c => c.Nombre).HasMaxLength(200).IsRequired();
        b.Property(c => c.ProfesorNombre).HasMaxLength(200).IsRequired();
        b.Property(c => c.PesoCorte1).HasPrecision(5, 2);
        b.Property(c => c.PesoCorte2).HasPrecision(5, 2);
        b.Property(c => c.PesoCorte3).HasPrecision(5, 2);
        b.HasIndex(c => c.ProfesorId);
    }
}

internal sealed class CursoEstudianteConfiguracion : IEntityTypeConfiguration<CursoEstudiante>
{
    public void Configure(EntityTypeBuilder<CursoEstudiante> b)
    {
        b.ToTable("CursoEstudiante");
        b.HasKey(ce => new { ce.CursoId, ce.EstudianteId });
        b.HasIndex(ce => ce.EstudianteId);
        b.HasOne<Curso>().WithMany().HasForeignKey(ce => ce.CursoId);
    }
}

internal sealed class ActividadConfiguracion : IEntityTypeConfiguration<Actividad>
{
    public void Configure(EntityTypeBuilder<Actividad> b)
    {
        b.ToTable("Actividad", t =>
        {
            t.HasCheckConstraint("CK_Actividad_Corte", "[Corte] BETWEEN 1 AND 3");
            t.HasCheckConstraint("CK_Actividad_Peso", "[Peso] >= 0 AND [Peso] <= 100");
        });
        b.HasKey(a => a.Id);
        b.Property(a => a.Id).ValueGeneratedNever();
        b.Property(a => a.Titulo).HasMaxLength(200).IsRequired();
        b.Property(a => a.Peso).HasPrecision(5, 2);
        b.Property(a => a.FechaLimite).HasColumnType("datetime2").HasConversion(Utc.Convertidor);
        b.HasIndex(a => a.CursoId);
        b.HasOne<Curso>().WithMany().HasForeignKey(a => a.CursoId);
    }
}

internal sealed class CalificacionConfiguracion : IEntityTypeConfiguration<Calificacion>
{
    public void Configure(EntityTypeBuilder<Calificacion> b)
    {
        b.ToTable("Calificacion", t =>
        {
            t.HasCheckConstraint("CK_Calificacion_Valor", "[Valor] >= 0 AND [Valor] <= 5");
            t.HasCheckConstraint("CK_Calificacion_Estado", "[Estado] IN ('BORRADOR', 'PUBLICADA')");
        });
        b.HasKey(c => c.Id);
        b.Property(c => c.Id).ValueGeneratedNever();
        b.Property(c => c.Valor).HasPrecision(4, 2);
        b.Property(c => c.Retroalimentacion).HasMaxLength(4000);
        b.Property(c => c.Estado).HasConversion<string>().HasMaxLength(10).IsUnicode(false);
        b.Property(c => c.Version).IsConcurrencyToken();
        b.HasIndex(c => new { c.ActividadId, c.EstudianteId }).IsUnique();
        b.HasIndex(c => c.EstudianteId);
        b.HasOne<Actividad>().WithMany().HasForeignKey(c => c.ActividadId);
    }
}

internal sealed class PublicacionCorteConfiguracion : IEntityTypeConfiguration<PublicacionCorte>
{
    public void Configure(EntityTypeBuilder<PublicacionCorte> b)
    {
        b.ToTable("PublicacionCorte", t =>
        {
            t.HasCheckConstraint("CK_PublicacionCorte_Corte", "[Corte] BETWEEN 1 AND 3");
            t.HasCheckConstraint("CK_PublicacionCorte_Nota", "[Nota] >= 0 AND [Nota] <= 5");
        });
        b.HasKey(p => p.Id);
        b.Property(p => p.Id).ValueGeneratedNever();
        b.Property(p => p.Nota).HasPrecision(7, 4);
        b.Property(p => p.FechaPublicacion).HasColumnType("datetime2").HasConversion(Utc.Convertidor);
        b.HasIndex(p => new { p.CursoId, p.EstudianteId, p.Corte, p.Version }).IsUnique();
        b.HasIndex(p => p.EstudianteId);
        b.HasOne<Curso>().WithMany().HasForeignKey(p => p.CursoId);
    }
}
