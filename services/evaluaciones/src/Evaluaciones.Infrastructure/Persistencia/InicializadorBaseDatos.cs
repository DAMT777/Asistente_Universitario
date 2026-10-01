using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Evaluaciones.Infrastructure.Persistencia;

public sealed class InicializadorBaseDatos(EvaluacionesDbContext db, ILogger<InicializadorBaseDatos> logger)
{
    public async Task AplicarMigracionesAsync(CancellationToken ct)
    {
        await db.Database.MigrateAsync(ct);
        logger.LogInformation("Migraciones de evaluaciones_db aplicadas");
    }
}
