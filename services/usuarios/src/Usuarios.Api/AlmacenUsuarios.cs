using Microsoft.AspNetCore.Identity;

namespace Usuarios.Api;

/// <summary>
/// Almacén de usuarios en memoria. Sustituye a usuarios_db mientras el frente A no entrega la base real.
/// Los datos semilla (sección 9.8) solo existen en desarrollo.
/// </summary>
public sealed class AlmacenUsuarios
{
    private const string ClaveDemo = "Demo1234!";

    private readonly PasswordHasher<Usuario> _hasher = new();
    private readonly List<Usuario> _usuarios = [];
    private readonly string _hashFalso;

    public AlmacenUsuarios(IHostEnvironment entorno)
    {
        // Sirve para gastar el mismo tiempo cuando el usuario no existe y no revelar cuáles existen.
        _hashFalso = _hasher.HashPassword(null!, Guid.NewGuid().ToString("N"));

        if (!entorno.IsDevelopment()) return;

        Agregar("a0000000-0000-0000-0000-000000000001", "1000000001", "P0001", "Profesor Demo", "profesor@demo.unillanos.edu.co", Roles.Profesor);
        Agregar("b0000000-0000-0000-0000-000000000001", "1000000011", "E0001", "Ana Demo", "ana@demo.unillanos.edu.co", Roles.Estudiante);
        Agregar("b0000000-0000-0000-0000-000000000002", "1000000012", "E0002", "Luis Demo", "luis@demo.unillanos.edu.co", Roles.Estudiante);
        Agregar("b0000000-0000-0000-0000-000000000003", "1000000013", "E0003", "Marta Demo", "marta@demo.unillanos.edu.co", Roles.Estudiante);
    }

    private void Agregar(string id, string documento, string codigo, string nombre, string correo, string rol)
    {
        var usuario = new Usuario(Guid.Parse(id), documento, codigo, nombre, correo, "", rol);
        _usuarios.Add(usuario with { PasswordHash = _hasher.HashPassword(usuario, ClaveDemo) });
    }

    /// <summary>Acepta código institucional, documento o correo. Devuelve null si las credenciales no son válidas.</summary>
    public Usuario? Autenticar(string identificador, string password)
    {
        var usuario = _usuarios.FirstOrDefault(u =>
            string.Equals(u.CodigoInstitucional, identificador, StringComparison.OrdinalIgnoreCase)
            || u.Documento == identificador
            || string.Equals(u.Correo, identificador, StringComparison.OrdinalIgnoreCase));

        if (usuario is null)
        {
            _hasher.VerifyHashedPassword(null!, _hashFalso, password);
            return null;
        }

        return _hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, password) == PasswordVerificationResult.Failed
            ? null
            : usuario;
    }

    public Usuario? Buscar(Guid id) => _usuarios.FirstOrDefault(u => u.Id == id);
}
