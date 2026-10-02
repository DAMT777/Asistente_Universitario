using Microsoft.EntityFrameworkCore;

namespace Usuarios.Api;

/// <summary>Esquema de usuarios_db (sección 9.2 de la guía técnica).</summary>
public class UsuariosDbContext(DbContextOptions<UsuariosDbContext> opciones) : DbContext(opciones)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder modelo)
    {
        modelo.Entity<Usuario>(e =>
        {
            e.ToTable("Usuario");
            e.HasKey(x => x.Id);
            e.Property(x => x.Documento).HasMaxLength(20).IsUnicode(false);
            e.Property(x => x.CodigoInstitucional).HasMaxLength(20).IsUnicode(false);
            e.Property(x => x.Nombre).HasMaxLength(150);
            e.Property(x => x.Correo).HasMaxLength(150);
            e.Property(x => x.PasswordHash).HasMaxLength(255);
            e.Property(x => x.Rol).HasColumnType("varchar(12)");
            e.HasIndex(x => x.Documento).IsUnique();
            e.HasIndex(x => x.CodigoInstitucional).IsUnique();
            e.HasIndex(x => x.Correo).IsUnique();
            e.ToTable(t => t.HasCheckConstraint("CK_Usuario_Rol", "Rol IN ('PROFESOR','ESTUDIANTE')"));
        });
    }
}
