namespace Usuarios.Api;

public static class Roles
{
    public const string Profesor = "PROFESOR";
    public const string Estudiante = "ESTUDIANTE";
}

/// <summary>Persona que inicia sesión (tabla Usuario de usuarios_db, sección 9.2 de la guía técnica).</summary>
public class Usuario
{
    public Guid Id { get; set; }
    public string Documento { get; set; } = "";
    public string CodigoInstitucional { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Correo { get; set; } = "";

    /// <summary>Hash con sal del PasswordHasher de ASP.NET Core Identity (sección 12.3). Nunca sale en una respuesta.</summary>
    public string PasswordHash { get; set; } = "";

    /// <summary>PROFESOR o ESTUDIANTE.</summary>
    public string Rol { get; set; } = "";
}

public sealed record SolicitudLogin(string? Usuario, string? Password);

/// <summary>Datos básicos del usuario. Nunca incluye el hash de la contraseña.</summary>
public sealed record UsuarioDto(Guid Id, string Nombre, string CodigoInstitucional, string Rol)
{
    public static UsuarioDto De(Usuario u) => new(u.Id, u.Nombre, u.CodigoInstitucional, u.Rol);
}
