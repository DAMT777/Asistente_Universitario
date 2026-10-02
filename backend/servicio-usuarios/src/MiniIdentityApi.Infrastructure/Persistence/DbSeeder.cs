using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MiniIdentityApi.Application.Interfaces;
using MiniIdentityApi.Domain.Entities;

namespace MiniIdentityApi.Infrastructure.Persistence;

/// <summary>
/// Crea las tablas si no existen y carga datos semilla (idempotente).
/// Los Guid de los usuarios son FIJOS para que los otros servicios
/// (evaluaciones, entregas) puedan referenciarlos en sus propios datos semilla.
/// </summary>
public static class DbSeeder
{
    public static readonly Guid ProfesorId    = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid Estudiante1Id = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid Estudiante2Id = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public static void Initialize(IServiceProvider services, ILogger logger)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        CreateDatabaseWithRetry(db, logger);

        var admin = EnsureRole(db, "Admin",
            ("users.read", "Can read users"), ("users.manage", "Can manage users"),
            ("roles.read", "Can read roles"), ("roles.manage", "Can manage roles"));
        var profesor = EnsureRole(db, "PROFESOR");
        var estudiante = EnsureRole(db, "ESTUDIANTE");

        EnsureUser(db, hasher, "admin", "admin@example.com", "Admin123*", "Administrador", null, null, admin, null);
        EnsureUser(db, hasher, "lrincon", "lrincon@unillanos.edu.co", "Pass123*", "Laura Rincón", "1000000001", "lrincon", profesor, ProfesorId);
        EnsureUser(db, hasher, "160005017", "160005017@unillanos.edu.co", "Pass123*", "Diego Alejandro Machado Tovar", "1000000002", "160005017", estudiante, Estudiante1Id);
        EnsureUser(db, hasher, "160005021", "160005021@unillanos.edu.co", "Pass123*", "Valentina Ortiz Rojas", "1000000003", "160005021", estudiante, Estudiante2Id);

        logger.LogInformation("Base de datos de usuarios lista.");
    }

    // El contenedor de Postgres puede tardar unos segundos en aceptar conexiones.
    private static void CreateDatabaseWithRetry(IdentityDbContext db, ILogger logger)
    {
        const int maxAttempts = 15;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                db.Database.EnsureCreated();
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                logger.LogWarning("Postgres no disponible (intento {Attempt}/{Max}): {Message}",
                    attempt, maxAttempts, ex.Message);
                Thread.Sleep(2000);
            }
        }
    }

    private static Role EnsureRole(IdentityDbContext db, string name, params (string Code, string Desc)[] permissions)
    {
        var role = db.Roles.Include(r => r.Permissions).FirstOrDefault(r => r.Name == name);
        if (role is not null) return role;

        role = new Role(name);
        foreach (var (code, desc) in permissions)
            role.AddPermission(new Permission(code, desc));

        db.Roles.Add(role);
        db.SaveChanges();
        return role;
    }

    private static void EnsureUser(IdentityDbContext db, IPasswordHasher hasher, string username, string email,
        string password, string fullName, string? document, string? code, Role role, Guid? id)
    {
        if (db.Users.Any(u => u.Username == username)) return;

        var salt = hasher.GenerateSalt();
        var credential = new Credential(hasher.Hash(password, salt), salt);
        var user = new User(username, email, credential, fullName, document, code, id);
        user.AddRole(role);

        db.Users.Add(user);
        db.SaveChanges();
    }
}
