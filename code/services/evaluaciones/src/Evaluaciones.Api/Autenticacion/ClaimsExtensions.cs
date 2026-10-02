using System.Security.Claims;
using Evaluaciones.Application;
using Evaluaciones.Domain;

namespace Evaluaciones.Api.Autenticacion;

public static class ClaimsExtensions
{
    /// <summary>Lee sub y rol del token. Con MapInboundClaims desactivado los claims conservan su nombre original.</summary>
    public static Actor ComoActor(this ClaimsPrincipal usuario)
    {
        var sub = usuario.FindFirst("sub")?.Value;
        var rol = usuario.FindFirst("rol")?.Value;
        if (!Guid.TryParse(sub, out var id) || string.IsNullOrWhiteSpace(rol))
            throw new NoAutenticadoException("El token no trae el identificador o el rol del usuario.");
        return new Actor(id, rol);
    }
}
