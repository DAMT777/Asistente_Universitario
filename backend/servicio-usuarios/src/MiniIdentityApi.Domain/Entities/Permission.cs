namespace MiniIdentityApi.Domain.Entities;

public class Permission
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Code { get; private set; } = null!;
    public string Description { get; private set; } = null!;

    private Permission() { } // requerido por EF Core

    public Permission(string code, string description)
    {
        Code = code;
        Description = description;
    }
}
