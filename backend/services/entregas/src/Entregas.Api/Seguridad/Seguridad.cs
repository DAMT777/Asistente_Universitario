using System.Security.Claims;
using Entregas.Domain;

namespace Entregas.Api.Seguridad;

public static class Politicas
{
    public const string Profesor = "Profesor";
    public const string Estudiante = "Estudiante";
}

/// <summary>Lee el id y el rol desde los claims del token (sub y rol, sección 12.1).</summary>
public static class UsuarioActual
{
    public static Guid ObtenerId(this ClaimsPrincipal usuario) =>
        Guid.TryParse(usuario.FindFirstValue("sub"), out var id)
            ? id
            : throw new DominioException(CodigosError.NoAutenticado, "El token no trae un identificador de usuario válido.");

    public static string ObtenerRol(this ClaimsPrincipal usuario) =>
        usuario.FindFirstValue("rol")
            ?? throw new DominioException(CodigosError.NoAutenticado, "El token no trae el rol del usuario.");
}
