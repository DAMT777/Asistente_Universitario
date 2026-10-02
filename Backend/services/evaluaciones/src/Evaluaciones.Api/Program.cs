using System.Security.Cryptography;
using Evaluaciones.Api.Middleware;
using Evaluaciones.Api.Seguridad;
using Evaluaciones.Application;
using Evaluaciones.Domain;
using Evaluaciones.Domain.Errores;
using Evaluaciones.Infrastructure;
using Evaluaciones.Infrastructure.Persistencia;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAplicacion();
builder.Services.AddInfraestructura(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks().AddDbContextCheck<EvaluacionesDbContext>("base-de-datos", tags: ["ready"]);

// El token lo firma el servicio de usuarios (RS256). Aquí solo se verifica con la llave pública (sección 12.1).
var jwt = builder.Configuration.GetSection("Jwt");
var rutaLlave = jwt["PublicKeyPath"];
if (string.IsNullOrWhiteSpace(rutaLlave))
    throw new InvalidOperationException("Falta Jwt:PublicKeyPath (variable Jwt__PublicKeyPath).");

var rsa = RSA.Create();
rsa.ImportFromPem(File.ReadAllText(Path.GetFullPath(rutaLlave, builder.Environment.ContentRootPath)));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opciones =>
    {
        opciones.MapInboundClaims = false; // conserva los nombres sub y rol tal como vienen en el token
        opciones.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwt["Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new RsaSecurityKey(rsa),
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
            NameClaimType = "sub",
            RoleClaimType = "rol",
            ClockSkew = TimeSpan.FromSeconds(30)
        };
        opciones.Events = new JwtBearerEvents
        {
            OnChallenge = contexto =>
            {
                contexto.HandleResponse();
                var expirado = contexto.AuthenticateFailure is SecurityTokenExpiredException;
                return RespuestaError.EscribirAsync(
                    contexto.HttpContext,
                    StatusCodes.Status401Unauthorized,
                    expirado ? CodigosError.TokenExpirado : CodigosError.NoAutenticado,
                    expirado ? "El token expiró. Inicia sesión de nuevo." : "Falta el token o no es válido.");
            },
            OnForbidden = contexto => RespuestaError.EscribirAsync(
                contexto.HttpContext,
                StatusCodes.Status403Forbidden,
                CodigosError.SinPermiso,
                "Tu rol no permite esta acción.")
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Politicas.Profesor, p => p.RequireRole(Roles.Profesor));

var app = builder.Build();

if (app.Environment.IsDevelopment() && DependenciaInfraestructura.UsaBaseEnMemoria(app.Configuration))
    await SemillaDesarrollo.AplicarAsync(app.Services, app.Services.GetRequiredService<TimeProvider>());

app.UseMiddleware<ManejadorErroresMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.MapControllers();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = r => r.Tags.Contains("ready") });

app.Run();

public partial class Program;
