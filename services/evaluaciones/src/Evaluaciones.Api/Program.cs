using Evaluaciones.Api;
using Evaluaciones.Api.Seguridad;
using Evaluaciones.Application;
using Evaluaciones.Infrastructure;
using Evaluaciones.Infrastructure.Persistencia;
using Unillanos.ServiceDefaults;
using Unillanos.ServiceDefaults.Errores;
using Unillanos.ServiceDefaults.Salud;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddAplicacionEvaluaciones();
builder.Services.AddInfraestructuraEvaluaciones(builder.Configuration);
builder.Services.AddClaveServicio();
builder.Services.AddTraductorExcepciones<TraductorExcepcionesEvaluaciones>();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<EvaluacionesDbContext>("sql", tags: [SaludExtensions.EtiquetaReady]);

var app = builder.Build();
app.UseServiceDefaults();

await using (var scope = app.Services.CreateAsyncScope())
{
    if (app.Configuration.GetValue<bool>("Database:AplicarMigraciones"))
        await scope.ServiceProvider.GetRequiredService<InicializadorBaseDatos>().AplicarMigracionesAsync(CancellationToken.None);
    if (app.Environment.IsDevelopment())
        await scope.ServiceProvider.GetRequiredService<SemillaDesarrollo>().CargarAsync(CancellationToken.None);
}

await app.RunAsync();
