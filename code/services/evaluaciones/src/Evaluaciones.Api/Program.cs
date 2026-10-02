using System.Security.Cryptography;
using Evaluaciones.Api.Autenticacion;
using Evaluaciones.Api.Errores;
using Evaluaciones.Api.Salud;
using Evaluaciones.Application.Calificaciones;
using Evaluaciones.Domain;
using Evaluaciones.Infrastructure;
using Evaluaciones.Infrastructure.Persistencia;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// --- Controladores y formato de errores (400 con el mismo formato que el resto de errores) ---
builder.Services.AddControllers().ConfigureApiBehaviorOptions(o =>
{
    o.InvalidModelStateResponseFactory = ctx =>
    {
        var campos = ctx.ModelState.Where(kv => kv.Value?.Errors.Count > 0).Select(kv => kv.Key.TrimStart('$', '.'));
        var detalle = string.Join(", ", campos.Where(c => c.Length > 0));
        var mensaje = "El cuerpo o los parámetros no cumplen el formato esperado" + (detalle.Length > 0 ? $" ({detalle})." : ".");
        return new ObjectResult(new RespuestaError(400, CodigosError.ValidacionFallida, mensaje, Respuestas.TraceId(ctx.HttpContext))) { StatusCode = 400 };
    };
});
builder.Services.AddExceptionHandler<ManejadorExcepciones>();
builder.Services.AddProblemDetails();

// --- CORS: permite que el frontend (Vite) se conecte al backend ---
builder.Services.AddCors(o => o.AddPolicy("frontend", p =>
    p.WithOrigins(
          config["Cors:Origenes"]?.Split(',') ?? ["http://localhost:5173"])
     .AllowAnyMethod()
     .AllowAnyHeader()
     .WithExposedHeaders("ETag")));
builder.Services.AddOpenApi();

// --- Casos de uso e infraestructura ---
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<GuardarCalificacion>();
builder.Services.AddScoped<PublicarCalificaciones>();
builder.Services.AddScoped<ListarCalificaciones>();
var cadena = config.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Falta ConnectionStrings:Default (variable ConnectionStrings__Default o dotnet user-secrets).");
builder.Services.AddEvaluacionesInfrastructure(cadena);

// --- Autenticación: JWT RS256 verificado con la llave pública, sin consultar al servicio de usuarios ---
var rsa = RSA.Create();
var pem = config["Jwt:PublicKeyPem"];
if (string.IsNullOrWhiteSpace(pem))
{
    var ruta = config["Jwt:PublicKeyPath"]
        ?? throw new InvalidOperationException("Falta Jwt:PublicKeyPath (o Jwt:PublicKeyPem).");
    pem = File.ReadAllText(Path.GetFullPath(ruta, builder.Environment.ContentRootPath));
}
rsa.ImportFromPem(pem);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.MapInboundClaims = false; // conserva "sub" y "rol" tal como vienen en el token
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = config["Jwt:Issuer"] ?? throw new InvalidOperationException("Falta Jwt:Issuer."),
        ValidateAudience = true,
        ValidAudience = config["Jwt:Audience"] ?? throw new InvalidOperationException("Falta Jwt:Audience."),
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new RsaSecurityKey(rsa),
        ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
        ClockSkew = TimeSpan.FromSeconds(30),
    };
    o.Events = new JwtBearerEvents
    {
        OnChallenge = ctx =>
        {
            ctx.HandleResponse();
            var expirado = ctx.AuthenticateFailure is SecurityTokenExpiredException;
            return Respuestas.EscribirAsync(ctx.HttpContext, StatusCodes.Status401Unauthorized,
                expirado ? "TOKEN_EXPIRADO" : CodigosError.NoAutenticado,
                expirado ? "El token expiró. Inicia sesión de nuevo." : "Falta el token o no es válido.",
                ctx.HttpContext.RequestAborted);
        },
    };
});
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Politicas.Profesor, p => p.RequireAuthenticatedUser().RequireClaim("rol", "PROFESOR"));
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, ResultadoAutorizacion>();

builder.Services.AddHealthChecks().AddCheck<ListoCheck>("evaluaciones_db", tags: ["ready"]);

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors("frontend");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Datos semilla solo en desarrollo. Requiere haber aplicado las migraciones (dotnet ef database update).
    using var scope = app.Services.CreateScope();
    try
    {
        await SeedDatos.AplicarAsync(scope.ServiceProvider.GetRequiredService<EvaluacionesDbContext>(), TimeProvider.System);
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "No se pudieron sembrar los datos de desarrollo. ¿Aplicaste las migraciones con 'dotnet ef database update'?");
    }
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = r => r.Tags.Contains("ready") });

app.Run();

// Necesario para WebApplicationFactory<Program> en las pruebas de integración.
public partial class Program;
