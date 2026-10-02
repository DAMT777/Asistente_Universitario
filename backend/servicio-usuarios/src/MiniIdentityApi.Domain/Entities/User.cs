using MiniIdentityApi.Domain.Enums;

namespace MiniIdentityApi.Domain.Entities;

public class User
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Username { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string? FullName { get; private set; }
    public string? Document { get; private set; }
    public string? InstitutionalCode { get; private set; }
    public UserStatus Status { get; private set; } = UserStatus.Active;
    public Credential Credential { get; private set; } = null!;
    public List<Role> Roles { get; private set; } = new();

    private User() { } // requerido por EF Core

    public User(
        string username,
        string email,
        Credential credential,
        string? fullName = null,
        string? document = null,
        string? institutionalCode = null,
        Guid? id = null)
    {
        if (id.HasValue) Id = id.Value;
        Username = username;
        Email = email;
        Credential = credential;
        FullName = fullName;
        Document = document;
        InstitutionalCode = institutionalCode;
    }

    public void Activate() => Status = UserStatus.Active;
    public void Deactivate() => Status = UserStatus.Inactive;
    public void Block() => Status = UserStatus.Blocked;

    public void AddRole(Role role)
    {
        if (Roles.Any(r => r.Name.Equals(role.Name, StringComparison.OrdinalIgnoreCase)))
            return;

        Roles.Add(role);
    }

    public bool HasPermission(string code)
    {
        return Roles.Any(role => role.Permissions.Any(p =>
            p.Code.Equals(code, StringComparison.OrdinalIgnoreCase)));
    }
}
