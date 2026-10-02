namespace Usuarios.Api;

public static class Roles
{
    public const string Profesor = "PROFESOR";
    public const string Estudiante = "ESTUDIANTE";
}

/// <summary>Persona que inicia sesión (sección 9.2 de la guía técnica).</summary>
public sealed record Usuario(
    Guid Id,
    string Documento,
    string CodigoInstitucional,
    string Nombre,
    string Correo,
    string PasswordHash,
    string Rol);

public sealed record SolicitudLogin(string? Usuario, string? Password);

/// <summary>Datos básicos del usuario. Nunca incluye el hash de la contraseña.</summary>
public sealed record UsuarioDto(Guid Id, string Nombre, string CodigoInstitucional, string Rol)
{
    public static UsuarioDto De(Usuario u) => new(u.Id, u.Nombre, u.CodigoInstitucional, u.Rol);
}
