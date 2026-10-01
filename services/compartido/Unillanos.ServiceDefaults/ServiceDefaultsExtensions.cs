using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Unillanos.ServiceDefaults.Correlacion;
using Unillanos.ServiceDefaults.Errores;
using Unillanos.ServiceDefaults.Salud;
using Unillanos.ServiceDefaults.Seguridad;

namespace Unillanos.ServiceDefaults;

public static class ServiceDefaultsExtensions
{
    public static WebApplicationBuilder AddServiceDefaults(this WebApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();
        builder.Logging.AddJsonConsole(o =>
        {
            o.IncludeScopes = true;
            o.UseUtcTimestamp = true;
            o.TimestampFormat = "O";
        });

        // Solo método, ruta, estado y duración: nunca encabezados (Authorization) ni cuerpos.
        builder.Services.AddHttpLogging(o => o.LoggingFields =
            HttpLoggingFields.RequestMethod | HttpLoggingFields.RequestPath |
            HttpLoggingFields.ResponseStatusCode | HttpLoggingFields.Duration);

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddTransient<PropagarCorrelacionHandler>();
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddManejoErrores();
        builder.Services.AddAutenticacionJwt();
        builder.Services.AddHealthChecks();
        builder.Services.AddControllers();
        builder.Services.AddOpenApi();
        return builder;
    }

    public static WebApplication UseServiceDefaults(this WebApplication app)
    {
        app.UseMiddleware<CorrelacionMiddleware>();
        app.UseHttpLogging();
        app.UseManejoErrores();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapEndpointsSalud();
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi().AllowAnonymous();
        }
        app.MapControllers();
        return app;
    }
}
