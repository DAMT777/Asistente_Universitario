using Microsoft.EntityFrameworkCore;
using MiniIdentityApi.Application.Interfaces;
using MiniIdentityApi.Domain.Entities;
using MiniIdentityApi.Infrastructure.Persistence;

namespace MiniIdentityApi.Infrastructure.Repositories;

public class EfRoleRepository : IRoleRepository
{
    private readonly IdentityDbContext _db;

    public EfRoleRepository(IdentityDbContext db) => _db = db;

    public Role? FindByName(string name)
    {
        var n = name.Trim().ToLower();
        return _db.Roles.Include(r => r.Permissions)
                        .FirstOrDefault(r => r.Name.ToLower() == n);
    }

    public List<Role> GetAll() =>
        _db.Roles.Include(r => r.Permissions).AsNoTracking().ToList();

    public void Save(Role role)
    {
        if (_db.Entry(role).State == EntityState.Detached)
            _db.Roles.Add(role);

        _db.SaveChanges();
    }
}
