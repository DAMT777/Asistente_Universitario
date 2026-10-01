using Microsoft.EntityFrameworkCore;
using Evaluaciones.Domain.Entidades;

namespace Evaluaciones.Infrastructure.Persistencia;

public sealed class EvaluacionesDbContext(DbContextOptions<EvaluacionesDbContext> options) : DbContext(options)
{
    public DbSet<Curso> Cursos => Set<Curso>();
    public DbSet<CursoEstudiante> CursoEstudiantes => Set<CursoEstudiante>();
    public DbSet<Actividad> Actividades => Set<Actividad>();
    public DbSet<Calificacion> Calificaciones => Set<Calificacion>();
    public DbSet<PublicacionCorte> PublicacionesCorte => Set<PublicacionCorte>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(EvaluacionesDbContext).Assembly);
}
