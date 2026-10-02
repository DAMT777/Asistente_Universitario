using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Evaluaciones.Infrastructure.Persistencia;

/// <summary>Solo para `dotnet ef migrations add`: crea el DbContext de SQL Server sin abrir conexión.</summary>
internal sealed class FabricaDiseno : IDesignTimeDbContextFactory<EvaluacionesDbContext>
{
    public EvaluacionesDbContext CreateDbContext(string[] args)
    {
        var cadena = Environment.GetEnvironmentVariable("ConnectionStrings__Default") ?? "Server=diseno;Database=evaluaciones_db";
        return new EvaluacionesDbContext(new DbContextOptionsBuilder<EvaluacionesDbContext>().UseSqlServer(cadena).Options);
    }
}
