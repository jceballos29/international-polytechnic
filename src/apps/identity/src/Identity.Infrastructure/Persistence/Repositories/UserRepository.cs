using Identity.Domain.Entities;
using Identity.Domain.Interfaces.Repositories;
using Identity.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IdentityDbContext _context;

    public UserRepository(IdentityDbContext context) => _context = context;

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _context.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<User?> GetByEmailAsync(
        Guid tenantId,
        Email email,
        CancellationToken ct = default
    ) =>
        await _context.Users.FirstOrDefaultAsync(
            u => u.TenantId == tenantId && u.Email == email,
            ct
        );

    public async Task<(IReadOnlyList<User> Users, int TotalCount)> GetAllAsync(
        Guid tenantId,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken ct = default
    )
    {
        var query = _context.Users.Where(u => u.TenantId == tenantId).OrderBy(u => u.CreatedAt);

        var totalCount = await query.CountAsync(ct);

        var users = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return (users, totalCount);
    }

    public async Task<bool> ExistsWithEmailAsync(
        Guid tenantId,
        Email email,
        CancellationToken ct = default
    ) => await _context.Users.AnyAsync(u => u.TenantId == tenantId && u.Email == email, ct);

    public async Task<Guid?> GetTenantIdByDomainAsync(string domain, CancellationToken ct = default)
    {
        var tenant = await _context
            .Tenants.Where(t => t.Domain == domain && t.IsActive)
            .Select(t => (Guid?)t.Id)
            .FirstOrDefaultAsync(ct);

        return tenant;
    }

    public async Task AddAsync(User user, CancellationToken ct = default) =>
        await _context.Users.AddAsync(user, ct);

    public Task UpdateAsync(User user, CancellationToken ct = default)
    {
        _context.Users.Update(user);
        return Task.CompletedTask;
    }
}
