using MiniIdentityApi.Domain.Entities;

namespace MiniIdentityApi.Application.DTOs.Users;

// Evita exponer Credential (hash y salt) en las respuestas de la API.
public class UserResponse
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? Document { get; set; }
    public string? InstitutionalCode { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();

    public static UserResponse From(User user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        FullName = user.FullName,
        Document = user.Document,
        InstitutionalCode = user.InstitutionalCode,
        Status = user.Status.ToString(),
        Roles = user.Roles.Select(r => r.Name).ToList()
    };
}
