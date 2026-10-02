using System.Security.Claims;

namespace Unillanos.ServiceDefaults.Seguridad;

public static class Roles
{
    public const string Profesor = "PROFESOR";
    public const string Estudiante = "ESTUDIANTE";
}

public static class Politicas
{
    public const string SoloEstudiante = "SoloEstudiante";
    public const string SoloProfesor = "SoloProfesor";
    public const string EstudianteOProfesor = "EstudianteOProfesor";
}

public static class ClaimsJwt
{
    public const string Sujeto = "sub";
    public const string Rol = "rol";
    public const string Nombre = "nombre";
}

public static class ClaimsPrincipalExtensions
{
    /// <summary>Id del usuario autenticado (claim sub). Lanza si el token no trae un GUID válido.</summary>
    public static bool EsProfesor(this ClaimsPrincipal usuario) => usuario.IsInRole(Roles.Profesor);

    public static Guid ObtenerUsuarioId(this ClaimsPrincipal usuario)
        => Guid.TryParse(usuario.FindFirstValue(ClaimsJwt.Sujeto), out var id) && id != Guid.Empty
            ? id
            : throw new UnauthorizedAccessException("Claim sub ausente o inválido.");
}
