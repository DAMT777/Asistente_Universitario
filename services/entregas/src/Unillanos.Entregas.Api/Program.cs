using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;
using Unillanos.Entregas.Api;
using Unillanos.Entregas.Application;
using Unillanos.Entregas.Application.Comun;
using Unillanos.Entregas.Infrastructure;
using Unillanos.Entregas.Infrastructure.Almacen;
using Unillanos.Entregas.Infrastructure.Persistencia;
using Unillanos.ServiceDefaults;
using Unillanos.ServiceDefaults.Correlacion;
using Unillanos.ServiceDefaults.Errores;
using Unillanos.ServiceDefaults.Salud;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddOptions<EntregasOpciones>()
    .BindConfiguration(EntregasOpciones.Seccion)
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Kestrel corta cuerpos muy grandes antes de leerlos (413). El margen cubre los encabezados multipart;
// el límite exacto del archivo lo valida el dominio, también con ARCHIVO_DEMASIADO_GRANDE.
const long MargenMultipart = 64 * 1024;
builder.Services.AddOptions<KestrelServerOptions>().Configure<IOptions<EntregasOpciones>>(
    (k, o) => k.Limits.MaxRequestBodySize = o.Value.MaxBytes + MargenMultipart);

builder.Services.AddAplicacionEntregas();
builder.Services.AddInfraestructuraEntregas(builder.Configuration);
builder.Services.AddClienteEvaluaciones().AddHttpMessageHandler<PropagarCorrelacionHandler>();
builder.Services.AddTraductorExcepciones<TraductorExcepcionesEntregas>();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<EntregasDbContext>("sql", tags: [SaludExtensions.EtiquetaReady])
    .AddCheck<BlobSaludCheck>("blob", tags: [SaludExtensions.EtiquetaReady]);

var app = builder.Build();
app.UseServiceDefaults();

if (app.Configuration.GetValue<bool>("Database:AplicarMigraciones"))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<InicializadorBaseDatos>().AplicarMigracionesAsync(CancellationToken.None);
}

await app.RunAsync();
