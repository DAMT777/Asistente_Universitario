using System.Diagnostics;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Usuarios.Api;

var builder = WebApplication.CreateBuilder(args);

var emisor = new EmisorToken(builder.Configuration, builder.Environment, TimeProvider.System);
builder.Services.AddSingleton(emisor);
builder.Services.AddSingleton<AlmacenUsuarios>();
builder.Services.AddHealthChecks();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opciones =>
    {
        opciones.MapInboundClaims = false;
        opciones.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = emisor.Emisor,
            ValidateAudience = true,
            ValidAudience = emisor.Audiencia,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = emisor.LlaveDeValidacion,
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
                        expirado ? "El token expiró. Inicia sesión de nuevo." : "Falta el token o no es válido.")
                    .ExecuteAsync(contexto.HttpContext);
            }
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// CU-01. Valida credenciales y devuelve el token con su rol.
app.MapPost("/auth/login", (SolicitudLogin solicitud, HttpContext http, AlmacenUsuarios almacen, EmisorToken emisor) =>
{
    if (string.IsNullOrWhiteSpace(solicitud.Usuario) || string.IsNullOrEmpty(solicitud.Password))
        return Error(http, 400, "VALIDACION_FALLIDA", "Escribe el usuario y la contraseña.");

    var usuario = almacen.Autenticar(solicitud.Usuario.Trim(), solicitud.Password);
    if (usuario is null)
        return Error(http, 401, "NO_AUTENTICADO", "Usuario o contraseña incorrectos.");

    return Results.Ok(new
    {
        accessToken = emisor.Emitir(usuario),
        tokenType = "Bearer",
        expiresIn = EmisorToken.MinutosDeVida * 60,
        usuario = UsuarioDto.De(usuario)
    });
}).AllowAnonymous();

// Datos básicos del usuario del token.
app.MapGet("/auth/me", (HttpContext http, AlmacenUsuarios almacen) =>
{
    var usuario = Guid.TryParse(http.User.FindFirst("sub")?.Value, out var id) ? almacen.Buscar(id) : null;
    return usuario is null
        ? Error(http, 401, "NO_AUTENTICADO", "El usuario del token ya no existe.")
        : Results.Ok(UsuarioDto.De(usuario));
}).RequireAuthorization();

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready");

app.Run();

// Formato de error común a todos los servicios (sección 8.2).
static IResult Error(HttpContext http, int status, string codigo, string mensaje) =>
    Results.Json(new { status, codigo, mensaje, traceId = Activity.Current?.Id ?? http.TraceIdentifier }, statusCode: status);

public partial class Program;
