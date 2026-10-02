using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Usuarios.Api;

/// <summary>
/// Firma el JWT con RS256 (sección 12.1). Los demás servicios verifican con la llave pública,
/// sin consultar a este servicio. Claims: sub, rol, nombre, iss, aud, iat y exp.
/// </summary>
public sealed class EmisorToken
{
    public const int MinutosDeVida = 60;

    private readonly RsaSecurityKey _llave;
    private readonly string _emisor;
    private readonly string _audiencia;
    private readonly TimeProvider _reloj;

    public EmisorToken(IConfiguration configuracion, IHostEnvironment entorno, TimeProvider reloj)
    {
        var ruta = configuracion["Jwt:PrivateKeyPath"];
        if (string.IsNullOrWhiteSpace(ruta))
            throw new InvalidOperationException("Falta Jwt:PrivateKeyPath (variable Jwt__PrivateKeyPath).");

        var rsa = RSA.Create();
        rsa.ImportFromPem(File.ReadAllText(Path.GetFullPath(ruta, entorno.ContentRootPath)));
        _llave = new RsaSecurityKey(rsa);
        _emisor = configuracion["Jwt:Issuer"] ?? "unillanos-usuarios";
        _audiencia = configuracion["Jwt:Audience"] ?? "unillanos-notas";
        _reloj = reloj;
    }

    public string Emisor => _emisor;
    public string Audiencia => _audiencia;
    public SecurityKey LlaveDeValidacion => _llave;

    public string Emitir(Usuario usuario)
    {
        var ahora = _reloj.GetUtcNow().UtcDateTime;
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _emisor,
            Audience = _audiencia,
            Subject = new ClaimsIdentity([
                new Claim("sub", usuario.Id.ToString()),
                new Claim("rol", usuario.Rol),
                new Claim("nombre", usuario.Nombre)
            ]),
            IssuedAt = ahora,
            NotBefore = ahora,
            Expires = ahora.AddMinutes(MinutosDeVida),
            SigningCredentials = new SigningCredentials(_llave, SecurityAlgorithms.RsaSha256)
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
