using Microsoft.EntityFrameworkCore;
using Entregas.Domain;

namespace Entregas.Infrastructure.Persistencia;

public sealed class EntregasDbContext(DbContextOptions<EntregasDbContext> options) : DbContext(options)
{
    public DbSet<Entrega> Entregas => Set<Entrega>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(EntregasDbContext).Assembly);
}
