using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Usuarios.Api;

/// <summary>
/// Usuarios de prueba de la sección 9.8 (contraseña Demo1234!). Solo en desarrollo e idempotente: si ya hay
/// usuarios no hace nada. Los identificadores son fijos porque los comparten los tres servicios.
/// Los códigos se guardan en mayúsculas y los correos en minúsculas, que es como se buscan al iniciar sesión.
/// </summary>
public static class SemillaUsuarios
{
    private const string ClaveDemo = "Demo1234!";

    public static async Task AplicarAsync(IServiceProvider servicios, CancellationToken ct = default)
    {
        using var alcance = servicios.CreateScope();
        var db = alcance.ServiceProvider.GetRequiredService<UsuariosDbContext>();

        if (db.Database.IsInMemory())
            await db.Database.EnsureCreatedAsync(ct);

        if (await db.Usuarios.AnyAsync(ct)) return;

        Agregar(db, "a0000000-0000-0000-0000-000000000001", "1000000001", "P0001", "Profesor Demo", "profesor@demo.unillanos.edu.co", Roles.Profesor);
        Agregar(db, "b0000000-0000-0000-0000-000000000001", "1000000011", "E0001", "Ana Demo", "ana@demo.unillanos.edu.co", Roles.Estudiante);
        Agregar(db, "b0000000-0000-0000-0000-000000000002", "1000000012", "E0002", "Luis Demo", "luis@demo.unillanos.edu.co", Roles.Estudiante);
        Agregar(db, "b0000000-0000-0000-0000-000000000003", "1000000013", "E0003", "Marta Demo", "marta@demo.unillanos.edu.co", Roles.Estudiante);

        await db.SaveChangesAsync(ct);
    }

    private static void Agregar(UsuariosDbContext db, string id, string documento, string codigo, string nombre, string correo, string rol)
    {
        var usuario = new Usuario
        {
            Id = Guid.Parse(id), Documento = documento, CodigoInstitucional = codigo.ToUpperInvariant(),
            Nombre = nombre, Correo = correo.ToLowerInvariant(), Rol = rol
        };
        usuario.PasswordHash = AlmacenUsuarios.Hashear(usuario, ClaveDemo);
        db.Usuarios.Add(usuario);
    }
}
