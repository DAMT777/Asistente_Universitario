using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;

namespace Unillanos.ServiceDefaults.Salud;

public static class SaludExtensions
{
    public const string EtiquetaReady = "ready";

    /// <summary>live: el proceso responde. ready: sus dependencias propias (BD, blob) responden.</summary>
    public static IEndpointRouteBuilder MapEndpointsSalud(this IEndpointRouteBuilder app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
        app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = c => c.Tags.Contains(EtiquetaReady) }).AllowAnonymous();
        return app;
    }
}
