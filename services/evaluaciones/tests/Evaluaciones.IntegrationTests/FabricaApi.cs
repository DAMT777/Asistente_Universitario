using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Evaluaciones.IntegrationTests;

/// <summary>
/// Levanta la API real en memoria con la base en memoria y datos semilla. Genera su propio par de llaves,
/// así que los tokens de prueba se firman igual que los del servicio de usuarios (RS256).
/// Cada instancia tiene su propia base, por lo que las pruebas no se afectan entre sí.
/// </summary>
public sealed class FabricaApi : WebApplicationFactory<Program>
{
    /// <summary>Llave de servicio de prueba para /internal/** (variable ServiceKey).</summary>
    public const string ClaveServicio = "clave-de-servicio-solo-para-pruebas";

    public static readonly Guid Profesor = new("a0000000-0000-0000-0000-000000000001");
    public static readonly Guid OtroProfesor = new("a0000000-0000-0000-0000-000000000002");
    public static readonly Guid Ana = new("b0000000-0000-0000-0000-000000000001");
    public static readonly Guid Luis = new("b0000000-0000-0000-0000-000000000002");
    public static readonly Guid Marta = new("b0000000-0000-0000-0000-000000000003");
    public static readonly Guid Curso = new("c0000000-0000-0000-0000-000000000001");
    public static readonly Guid Taller1 = new("d0000000-0000-0000-0000-000000000001");
    public static readonly Guid Parcial1 = new("d0000000-0000-0000-0000-000000000002");
    public static readonly Guid Proyecto1 = new("d0000000-0000-0000-0000-000000000003");

    private readonly RSA _llave = RSA.Create(2048);
    private readonly string _rutaPublica = Path.Combine(Path.GetTempPath(), $"jwt-public-{Guid.NewGuid():N}.pem");

    private readonly string? _cadenaSql;

    /// <param name="cadenaSql">Si se indica, usa ese SQL Server real (con migraciones) en vez de la base en memoria.</param>
    public FabricaApi(string? cadenaSql = null)
    {
        _cadenaSql = cadenaSql;
        File.WriteAllText(_rutaPublica, _llave.ExportSubjectPublicKeyInfoPem());
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        if (_cadenaSql is null)
        {
            builder.UseSetting("Database:UseInMemory", "true");
            builder.UseSetting("Database:InMemoryName", Guid.NewGuid().ToString("N"));
        }
        else
        {
            builder.UseSetting("Database:UseInMemory", "false");
            builder.UseSetting("ConnectionStrings:Default", _cadenaSql);
            builder.UseSetting("Database:AplicarMigraciones", "true");
        }
        builder.UseSetting("Jwt:PublicKeyPath", _rutaPublica);
        builder.UseSetting("ServiceKey", ClaveServicio);
    }

    public string Token(Guid usuarioId, string rol, TimeSpan? vigencia = null, RSA? firmadoCon = null)
    {
        var ahora = DateTime.UtcNow;
        var vence = ahora.Add(vigencia ?? TimeSpan.FromMinutes(30));
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = "unillanos-usuarios",
            Audience = "unillanos-notas",
            Subject = new ClaimsIdentity([
                new Claim("sub", usuarioId.ToString()),
                new Claim("rol", rol),
                new Claim("nombre", "Usuario de prueba")
            ]),
            NotBefore = ahora.AddMinutes(-10),
            IssuedAt = ahora.AddMinutes(-10),
            Expires = vence,
            SigningCredentials = new SigningCredentials(new RsaSecurityKey(firmadoCon ?? _llave), SecurityAlgorithms.RsaSha256)
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    public HttpClient ClienteDe(Guid usuarioId, string rol)
    {
        var cliente = CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(usuarioId, rol));
        return cliente;
    }

    public HttpClient ClienteProfesor() => ClienteDe(Profesor, "PROFESOR");
    public HttpClient ClienteEstudiante(Guid estudianteId) => ClienteDe(estudianteId, "ESTUDIANTE");

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;
        _llave.Dispose();
        try { File.Delete(_rutaPublica); } catch (IOException) { /* el archivo temporal no es crítico */ }
    }
}
