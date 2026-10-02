using Microsoft.EntityFrameworkCore;
using MiniIdentityApi.Application.Interfaces;
using MiniIdentityApi.Domain.Entities;
using MiniIdentityApi.Infrastructure.Persistence;

namespace MiniIdentityApi.Infrastructure.Repositories;

public class EfUserRepository : IUserRepository
{
    private readonly IdentityDbContext _db;

    public EfUserRepository(IdentityDbContext db) => _db = db;

    private IQueryable<User> UsersWithRoles() =>
        _db.Users.Include(u => u.Roles).ThenInclude(r => r.Permissions);

    public User? FindById(Guid id) =>
        UsersWithRoles().FirstOrDefault(u => u.Id == id);

    public User? FindByUsernameOrEmail(string value)
    {
        var v = value.Trim().ToLower();
        return UsersWithRoles().FirstOrDefault(u =>
            u.Username.ToLower() == v || u.Email.ToLower() == v);
    }

    public List<User> GetAll() => UsersWithRoles().AsNoTracking().ToList();

    public void Save(User user)
    {
        // Nuevo: se agrega. Ya rastreado (consultado en este mismo request): solo se guardan los cambios.
        if (_db.Entry(user).State == EntityState.Detached)
            _db.Users.Add(user);

        _db.SaveChanges();
    }
}
