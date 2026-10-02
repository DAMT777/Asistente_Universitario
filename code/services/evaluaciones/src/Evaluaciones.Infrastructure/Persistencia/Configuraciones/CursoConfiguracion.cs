using Evaluaciones.Domain.Cursos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Evaluaciones.Infrastructure.Persistencia.Configuraciones;

internal sealed class CursoConfiguracion : IEntityTypeConfiguration<Curso>
{
    public void Configure(EntityTypeBuilder<Curso> b)
    {
        b.ToTable("Curso", t => t.HasCheckConstraint("CK_Curso_PesosCortes", "[PesoCorte1] + [PesoCorte2] + [PesoCorte3] = 100"));
        b.HasKey(c => c.Id);
        b.Property(c => c.Id).ValueGeneratedNever();
        b.Property(c => c.Codigo).HasMaxLength(20).IsUnicode(false).IsRequired();
        b.Property(c => c.Nombre).HasMaxLength(150).IsRequired();
        b.Property(c => c.ProfesorId).IsRequired();
        b.Property(c => c.ProfesorNombre).HasMaxLength(150).IsRequired();
        b.Property(c => c.PesoCorte1).HasPrecision(5, 2);
        b.Property(c => c.PesoCorte2).HasPrecision(5, 2);
        b.Property(c => c.PesoCorte3).HasPrecision(5, 2);
    }
}

internal sealed class CursoEstudianteConfiguracion : IEntityTypeConfiguration<CursoEstudiante>
{
    public void Configure(EntityTypeBuilder<CursoEstudiante> b)
    {
        b.ToTable("CursoEstudiante");
        b.HasKey(x => new { x.CursoId, x.EstudianteId });
        b.Property(x => x.EstudianteNombre).HasMaxLength(150).IsRequired();
        b.Property(x => x.EstudianteCodigo).HasMaxLength(20).IsUnicode(false).IsRequired();
        b.HasOne<Curso>().WithMany().HasForeignKey(x => x.CursoId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.EstudianteId);
    }
}

internal sealed class ActividadConfiguracion : IEntityTypeConfiguration<Actividad>
{
    public void Configure(EntityTypeBuilder<Actividad> b)
    {
        b.ToTable("Actividad", t =>
        {
            t.HasCheckConstraint("CK_Actividad_Corte", "[Corte] IN (1,2,3)");
            t.HasCheckConstraint("CK_Actividad_Peso", "[Peso] > 0 AND [Peso] <= 100");
        });
        b.HasKey(a => a.Id);
        b.Property(a => a.Id).ValueGeneratedNever();
        b.Property(a => a.Titulo).HasMaxLength(150).IsRequired();
        b.Property(a => a.Corte).HasColumnType("tinyint");
        b.Property(a => a.Peso).HasPrecision(5, 2);
        b.Property(a => a.FechaLimite).HasColumnType("datetime2");
        b.HasOne<Curso>().WithMany().HasForeignKey(a => a.CursoId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(a => new { a.CursoId, a.Corte });
    }
}
