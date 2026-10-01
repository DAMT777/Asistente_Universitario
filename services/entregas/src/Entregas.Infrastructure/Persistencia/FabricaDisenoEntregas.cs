using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Entregas.Infrastructure.Persistencia;

/// <summary>Solo para `dotnet ef migrations add`: no abre conexión.</summary>
internal sealed class FabricaDisenoEntregas : IDesignTimeDbContextFactory<EntregasDbContext>
{
    public EntregasDbContext CreateDbContext(string[] args)
    {
        var cadena = Environment.GetEnvironmentVariable("ConnectionStrings__Default") ?? "Server=diseno;Database=entregas_db";
        return new EntregasDbContext(new DbContextOptionsBuilder<EntregasDbContext>().UseSqlServer(cadena).Options);
    }
}
