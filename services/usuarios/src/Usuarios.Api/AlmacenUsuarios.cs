using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Usuarios.Api;

/// <summary>
/// CU-01. Busca al usuario en usuarios_db y verifica la contraseña con el hasher de ASP.NET Core Identity
/// (sección 12.3). Acepta código institucional, documento o correo.
/// </summary>
public sealed class AlmacenUsuarios(UsuariosDbContext db)
{
    private static readonly PasswordHasher<Usuario> Hasher = new();

    // Sirve para gastar el mismo tiempo cuando el usuario no existe y no revelar cuáles existen.
    private static readonly string HashFalso = Hasher.HashPassword(new Usuario(), Guid.NewGuid().ToString("N"));

    public static string Hashear(Usuario usuario, string password) => Hasher.HashPassword(usuario, password);

    /// <returns>Null si las credenciales no son válidas, sin distinguir la causa.</returns>
    public async Task<Usuario?> AutenticarAsync(string identificador, string password, CancellationToken ct)
    {
        var codigo = identificador.ToUpperInvariant();
        var correo = identificador.ToLowerInvariant();
        var usuario = await db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u =>
            u.CodigoInstitucional == codigo
            || u.Documento == identificador
            || u.Correo == correo, ct);

        if (usuario is null)
        {
            Hasher.VerifyHashedPassword(new Usuario(), HashFalso, password);
            return null;
        }

        return Hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, password) == PasswordVerificationResult.Failed
            ? null
            : usuario;
    }

    public Task<Usuario?> BuscarAsync(Guid id, CancellationToken ct) =>
        db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);
}
