using Evaluaciones.Domain.Calificaciones;
using Evaluaciones.Domain.Cursos;
using Microsoft.EntityFrameworkCore;

namespace Evaluaciones.Infrastructure.Persistencia;

public sealed class EvaluacionesDbContext(DbContextOptions<EvaluacionesDbContext> options) : DbContext(options)
{
    public DbSet<Curso> Cursos => Set<Curso>();
    public DbSet<CursoEstudiante> CursoEstudiantes => Set<CursoEstudiante>();
    public DbSet<Actividad> Actividades => Set<Actividad>();
    public DbSet<Calificacion> Calificaciones => Set<Calificacion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EvaluacionesDbContext).Assembly);
}
