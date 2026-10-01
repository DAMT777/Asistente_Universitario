using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entregas.Infrastructure.Persistencia;

/// <summary>
/// Aplica las migraciones pendientes. Entregas no tiene datos semilla: las entregas solo
/// se crean subiendo archivos (así el blob y la fila siempre existen juntos).
/// </summary>
public sealed class InicializadorBaseDatos(EntregasDbContext db, ILogger<InicializadorBaseDatos> logger)
{
    public async Task AplicarMigracionesAsync(CancellationToken ct)
    {
        await db.Database.MigrateAsync(ct);
        logger.LogInformation("Migraciones de entregas_db aplicadas");
    }
}
