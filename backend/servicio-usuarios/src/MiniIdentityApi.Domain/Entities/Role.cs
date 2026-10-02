namespace MiniIdentityApi.Domain.Entities;

public class Role
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = null!;
    public List<Permission> Permissions { get; private set; } = new();

    private Role() { } // requerido por EF Core

    public Role(string name)
    {
        Name = name;
    }

    public void AddPermission(Permission permission)
    {
        if (Permissions.Any(p => p.Code.Equals(permission.Code, StringComparison.OrdinalIgnoreCase)))
            return;

        Permissions.Add(permission);
    }
}
