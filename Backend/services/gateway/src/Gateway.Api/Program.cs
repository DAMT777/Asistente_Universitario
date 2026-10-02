using System.Diagnostics;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Yarp.ReverseProxy.Forwarder;

const string EncabezadoCorrelacion = "X-Correlation-Id";

var builder = WebApplication.CreateBuilder(args);

// El gateway solo verifica el token con la llave pública (sección 12.1). Nunca firma.
var rutaLlave = builder.Configuration["Jwt:PublicKeyPath"];
if (string.IsNullOrWhiteSpace(rutaLlave))
    throw new InvalidOperationException("Falta Jwt:PublicKeyPath (variable Jwt__PublicKeyPath).");

var rsa = RSA.Create();
rsa.ImportFromPem(File.ReadAllText(Path.GetFullPath(rutaLlave, builder.Environment.ContentRootPath)));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opciones =>
    {
        opciones.MapInboundClaims = false;
        opciones.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
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
                return Error(contexto.HttpContext, 401,
                    expirado ? "TOKEN_EXPIRADO" : "NO_AUTENTICADO",
                    expirado ? "El token expiró. Inicia sesión de nuevo." : "Falta el token o no es válido.");
            }
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("publico", p => p.RequireAssertion(_ => true))
    .AddPolicy("autenticado", p => p.RequireAuthenticatedUser());

// CORS solo para los orígenes configurados del frontend (sección 7.5).
var origenes = builder.Configuration.GetSection("Cors:AllowedOrigins").GetChildren()
    .Select(o => o.Value)
    .Where(o => !string.IsNullOrWhiteSpace(o))
    .Cast<string>()
    .ToArray();
builder.Services.AddCors(o => o.AddPolicy("frontend", p => p
    .WithOrigins(origenes)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .WithExposedHeaders(EncabezadoCorrelacion, "ETag")));

builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
builder.Services.AddHealthChecks();

var app = builder.Build();

// Agrega o propaga el identificador de correlación para seguir una petición entre servicios.
app.Use(async (contexto, siguiente) =>
{
    var id = contexto.Request.Headers[EncabezadoCorrelacion].FirstOrDefault();
    if (string.IsNullOrWhiteSpace(id))
    {
        id = Guid.NewGuid().ToString("N");
        contexto.Request.Headers[EncabezadoCorrelacion] = id;
    }
    contexto.Response.Headers[EncabezadoCorrelacion] = id;
    await siguiente();
});

// /internal/** nunca se expone al exterior (sección 12.4).
app.Use(async (contexto, siguiente) =>
{
    if (contexto.Request.Path.StartsWithSegments("/internal"))
    {
        await Error(contexto, 404, "NO_ENCONTRADO", "La ruta no existe.");
        return;
    }
    await siguiente();
});

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health/live");

app.MapReverseProxy(proxy =>
{
    // Si el servicio de destino no responde, el cliente recibe el error común y no una respuesta vacía (RN-17).
    proxy.Use(async (contexto, siguiente) =>
    {
        await siguiente();
        var falla = contexto.Features.Get<IForwarderErrorFeature>();
        if (falla is not null && !contexto.Response.HasStarted)
            await Error(contexto, 503, "SERVICIO_NO_DISPONIBLE", "Un servicio necesario no respondió a tiempo. Intenta de nuevo.");
    });
    proxy.UseSessionAffinity();
    proxy.UseLoadBalancing();
    proxy.UsePassiveHealthChecks();
});

app.Run();

// Formato de error común a todos los servicios (sección 8.2).
static Task Error(HttpContext http, int status, string codigo, string mensaje)
{
    http.Response.StatusCode = status;
    return http.Response.WriteAsJsonAsync(new { status, codigo, mensaje, traceId = Activity.Current?.Id ?? http.TraceIdentifier });
}

public partial class Program;
