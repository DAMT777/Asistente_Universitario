using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Unillanos.Pruebas.Compartidas;

/// <summary>
/// Par de llaves RSA efímero para pruebas: la llave pública se escribe en un archivo temporal
/// (Jwt__PublicKeyPath) y la privada firma tokens RS256 como lo haría el servicio de Usuarios.
/// </summary>
public sealed class LlavesPrueba : IDisposable
{
    public const string Issuer = "unillanos-usuarios";
    public const string Audience = "unillanos-notas";

    private readonly RSA _rsa = RSA.Create(2048);
    private readonly JsonWebTokenHandler _emisor = new();

    public LlavesPrueba()
    {
        RutaLlavePublica = Path.Combine(Path.GetTempPath(), $"jwt-publica-{Guid.NewGuid():N}.pem");
        File.WriteAllText(RutaLlavePublica, _rsa.ExportSubjectPublicKeyInfoPem());
    }

    public string RutaLlavePublica { get; }

    public string CrearToken(Guid usuarioId, string rol, string nombre, bool expirado = false, RSA? firmarCon = null)
    {
        var ahora = DateTime.UtcNow;
        var emitido = expirado ? ahora.AddHours(-2) : ahora;
        return _emisor.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            IssuedAt = emitido,
            NotBefore = emitido,
            Expires = emitido.AddMinutes(60),
            Claims = new Dictionary<string, object>
            {
                ["sub"] = usuarioId.ToString(),
                ["rol"] = rol,
                ["nombre"] = nombre,
            },
            SigningCredentials = new SigningCredentials(new RsaSecurityKey(firmarCon ?? _rsa), SecurityAlgorithms.RsaSha256),
        });
    }

    public void Dispose()
    {
        _rsa.Dispose();
        File.Delete(RutaLlavePublica);
    }
}

public static class UsuariosSemilla
{
    public static readonly Guid Profesor = Guid.Parse("a0000000-0000-0000-0000-000000000001");
    public static readonly Guid Ana = Guid.Parse("b0000000-0000-0000-0000-000000000001");
    public static readonly Guid Luis = Guid.Parse("b0000000-0000-0000-0000-000000000002");
    public static readonly Guid Marta = Guid.Parse("b0000000-0000-0000-0000-000000000003");
    /// <summary>Estudiante que no está inscrito en el curso semilla.</summary>
    public static readonly Guid Pedro = Guid.Parse("b0000000-0000-0000-0000-000000000099");
    public static readonly Guid Curso = Guid.Parse("c0000000-0000-0000-0000-000000000001");
    public static readonly Guid Taller1 = Guid.Parse("d0000000-0000-0000-0000-000000000001");
    public static readonly Guid Parcial1 = Guid.Parse("d0000000-0000-0000-0000-000000000002");
    public static readonly Guid Proyecto1 = Guid.Parse("d0000000-0000-0000-0000-000000000003");
}
