using System.Diagnostics;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using Usuarios.Api;

var builder = WebApplication.CreateBuilder(args);

var emisor = new EmisorToken(builder.Configuration, builder.Environment, TimeProvider.System);
builder.Services.AddSingleton(emisor);
builder.Services.AddScoped<AlmacenUsuarios>();

// usuarios_db. En desarrollo, Database:UseInMemory usa una base en memoria con los usuarios de prueba.
if (bool.TryParse(builder.Configuration["Database:UseInMemory"], out var enMemoria) && enMemoria)
{
    var nombre = builder.Configuration["Database:InMemoryName"] ?? "usuarios_db";
    builder.Services.AddDbContext<UsuariosDbContext>(o => o.UseInMemoryDatabase(nombre));
}
else
{
    var conexion = builder.Configuration.GetConnectionString("Default")
        ?? throw new InvalidOperationException("Falta ConnectionStrings:Default (variable ConnectionStrings__Default).");
    builder.Services.AddDbContext<UsuariosDbContext>(o => o.UseSqlServer(conexion));
}
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

// Los usuarios de prueba solo existen en desarrollo (sección 9.8).
if (app.Environment.IsDevelopment())
    await SemillaUsuarios.AplicarAsync(app.Services);
else if (enMemoria)
    await EnsureCreatedAsync(app.Services);

app.UseAuthentication();
app.UseAuthorization();

// CU-01. Valida credenciales y devuelve el token con su rol.
app.MapPost("/auth/login", async (SolicitudLogin solicitud, HttpContext http, AlmacenUsuarios almacen, EmisorToken emisor, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(solicitud.Usuario) || string.IsNullOrEmpty(solicitud.Password))
        return Error(http, 400, "VALIDACION_FALLIDA", "Escribe el usuario y la contraseña.");

    var usuario = await almacen.AutenticarAsync(solicitud.Usuario.Trim(), solicitud.Password, ct);
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
app.MapGet("/auth/me", async (HttpContext http, AlmacenUsuarios almacen, CancellationToken ct) =>
{
    var usuario = Guid.TryParse(http.User.FindFirst("sub")?.Value, out var id) ? await almacen.BuscarAsync(id, ct) : null;
    return usuario is null
        ? Error(http, 401, "NO_AUTENTICADO", "El usuario del token ya no existe.")
        : Results.Ok(UsuarioDto.De(usuario));
}).RequireAuthorization();

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready");

app.Run();

static async Task EnsureCreatedAsync(IServiceProvider servicios)
{
    using var alcance = servicios.CreateScope();
    await alcance.ServiceProvider.GetRequiredService<UsuariosDbContext>().Database.EnsureCreatedAsync();
}

// Formato de error común a todos los servicios (sección 8.2).
static IResult Error(HttpContext http, int status, string codigo, string mensaje) =>
    Results.Json(new { status, codigo, mensaje, traceId = Activity.Current?.Id ?? http.TraceIdentifier }, statusCode: status);

public partial class Program;
