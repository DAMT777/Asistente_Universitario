using Evaluaciones.Infrastructure.Persistencia;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Evaluaciones.Api.Salud;

/// <summary>GET /health/ready: el servicio puede hablar con su base de datos.</summary>
public sealed class ListoCheck(EvaluacionesDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default) =>
        await db.Database.CanConnectAsync(ct)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("No hay conexión con evaluaciones_db.");
}
