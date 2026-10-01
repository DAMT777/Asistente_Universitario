using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Unillanos.ServiceDefaults.Errores;

namespace Unillanos.ServiceDefaults.Seguridad;

public static class AutenticacionExtensions
{
    /// <summary>Verifica JWT RS256 con la llave pública de Usuarios, sin consultar a Usuarios.</summary>
    public static IServiceCollection AddAutenticacionJwt(this IServiceCollection services)
    {
        services.AddOptions<JwtOpciones>()
            .BindConfiguration(JwtOpciones.Seccion)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<LlavePublicaJwt>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOpciones>, LlavePublicaJwt>((o, jwt, llave) =>
            {
                o.MapInboundClaims = false;
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Value.Issuer,
                    ValidAudience = jwt.Value.Audience,
                    IssuerSigningKey = llave.Llave,
                    ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
                    RequireExpirationTime = true,
                    RequireSignedTokens = true,
                    ClockSkew = TimeSpan.FromSeconds(jwt.Value.ClockSkewSegundos),
                    NameClaimType = ClaimsJwt.Nombre,
                    RoleClaimType = ClaimsJwt.Rol,
                };
                o.Events = new JwtBearerEvents
                {
                    OnChallenge = contexto =>
                    {
                        contexto.HandleResponse();
                        return contexto.AuthenticateFailure is SecurityTokenExpiredException
                            ? EscritorErrores.EscribirAsync(contexto.HttpContext, CodigosError.TokenExpirado, "El token expiró.")
                            : EscritorErrores.EscribirAsync(contexto.HttpContext, CodigosError.NoAutenticado, "Se requiere un token válido.");
                    },
                    OnForbidden = contexto =>
                        EscritorErrores.EscribirAsync(contexto.HttpContext, CodigosError.SinPermiso, "No tiene permiso para este recurso."),
                };
            });

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(Politicas.SoloEstudiante, p => p.RequireAuthenticatedUser().RequireRole(Roles.Estudiante));

        return services;
    }
}

/// <summary>Carga una sola vez la llave pública PEM indicada en Jwt__PublicKeyPath.</summary>
public sealed class LlavePublicaJwt(IOptions<JwtOpciones> opciones) : IDisposable
{
    private readonly Lazy<(RSA Rsa, RsaSecurityKey Llave)> _carga = new(() =>
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(File.ReadAllText(opciones.Value.PublicKeyPath));
        return (rsa, new RsaSecurityKey(rsa));
    });

    public RsaSecurityKey Llave => _carga.Value.Llave;

    public void Dispose()
    {
        if (_carga.IsValueCreated) _carga.Value.Rsa.Dispose();
    }
}
