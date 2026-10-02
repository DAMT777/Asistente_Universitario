using Microsoft.EntityFrameworkCore;
using MiniIdentityApi.Domain.Entities;

namespace MiniIdentityApi.Infrastructure.Persistence;

public class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("usuarios");
            e.HasKey(x => x.Id);
            // Los Guid se generan en el dominio; sin esto EF trataria las entidades nuevas como existentes.
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.Username).HasColumnName("username").HasMaxLength(100).IsRequired();
            e.Property(x => x.Email).HasColumnName("correo").HasMaxLength(200).IsRequired();
            e.Property(x => x.FullName).HasColumnName("nombre").HasMaxLength(200);
            e.Property(x => x.Document).HasColumnName("documento").HasMaxLength(30);
            e.Property(x => x.InstitutionalCode).HasColumnName("codigo_institucional").HasMaxLength(30);
            e.Property(x => x.Status).HasColumnName("estado").HasConversion<string>().HasMaxLength(20);
            e.HasIndex(x => x.Username).IsUnique();
            e.HasIndex(x => x.Email).IsUnique();

            e.OwnsOne(x => x.Credential, c =>
            {
                c.Property(x => x.PasswordHash).HasColumnName("password_hash").IsRequired();
                c.Property(x => x.Salt).HasColumnName("salt").IsRequired();
                c.Property(x => x.LastChangedAt).HasColumnName("password_changed_at");
            });

            // Muchos a muchos: un usuario puede tener varios roles.
            e.HasMany(x => x.Roles)
             .WithMany()
             .UsingEntity<Dictionary<string, object>>(
                 "usuario_roles",
                 j => j.HasOne<Role>().WithMany().HasForeignKey("role_id"),
                 j => j.HasOne<User>().WithMany().HasForeignKey("usuario_id"));
        });

        modelBuilder.Entity<Role>(e =>
        {
            e.ToTable("roles");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.Name).HasColumnName("nombre").HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.Name).IsUnique();
            e.HasMany(x => x.Permissions).WithOne().OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Permission>(e =>
        {
            e.ToTable("permisos");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.Code).HasColumnName("codigo").HasMaxLength(100).IsRequired();
            e.Property(x => x.Description).HasColumnName("descripcion").HasMaxLength(250);
        });
    }
}
