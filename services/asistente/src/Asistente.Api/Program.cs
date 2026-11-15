using System.Threading.RateLimiting;
using Asistente.Api;
using Asistente.Application;
using Asistente.Domain;
using Asistente.Infrastructure;
using Asistente.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Unillanos.ServiceDefaults;
using Unillanos.ServiceDefaults.Errores;
using Unillanos.ServiceDefaults.Salud;
using Unillanos.ServiceDefaults.Seguridad;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddOptions<AsistenteOpciones>().BindConfiguration(AsistenteOpciones.Seccion);
builder.Services.AddAplicacionAsistente();
builder.Services.AddInfraestructuraAsistente(builder.Configuration);
builder.Services.AddTraductorExcepciones<TraductorExcepcionesAsistente>();
builder.Services.AddHealthChecks().AddDbContextCheck<AsistenteDbContext>("sql", tags: [SaludExtensions.EtiquetaReady]);

// Límite de mensajes por usuario (sub) por minuto. Solo aplica a POST /asistente/mensajes.
var mensajesPorMinuto = builder.Configuration.GetValue("Asistente:MensajesPorMinuto", 10);
builder.Services.AddRateLimiter(o =>
{
    o.AddPolicy(LimiteMensajes.Politica, http => RateLimitPartition.GetFixedWindowLimiter(
        http.User.FindFirst(ClaimsJwt.Sujeto)?.Value ?? "anonimo",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = mensajesPorMinuto, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    o.OnRejected = (contexto, _) => new ValueTask(EscritorErrores.EscribirAsync(contexto.HttpContext,
        CodigosAsistente.LimiteMensajes, "Enviaste demasiados mensajes. Espera un minuto.", 429));
});

var app = builder.Build();
app.UseServiceDefaults();
app.UseRateLimiter();

if (app.Configuration.GetValue<bool>("Database:AplicarMigraciones"))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<AsistenteDbContext>().Database.MigrateAsync();
}

await app.RunAsync();

public partial class Program;
