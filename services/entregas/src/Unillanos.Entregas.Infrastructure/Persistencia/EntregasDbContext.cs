using Microsoft.EntityFrameworkCore;
using Unillanos.Entregas.Domain;

namespace Unillanos.Entregas.Infrastructure.Persistencia;

public sealed class EntregasDbContext(DbContextOptions<EntregasDbContext> options) : DbContext(options)
{
    public DbSet<Entrega> Entregas => Set<Entrega>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(EntregasDbContext).Assembly);
}
