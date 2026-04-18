using Identity.Domain.Entities;
using Identity.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistence.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly IdentityDbContext _context;

    public RoleRepository(IdentityDbContext context) => _context = context;

    public async Task<Role?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _context.Roles.Include(r => r.Permissions).FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<Role?> GetByNameAsync(
        Guid clientApplicationId,
        string name,
        CancellationToken ct = default
    ) =>
        await _context.Roles.FirstOrDefaultAsync(
            r => r.ClientApplicationId == clientApplicationId && r.Name == name.ToLowerInvariant(),
            ct
        );

    public async Task<IReadOnlyList<Role>> GetByApplicationAsync(
        Guid clientApplicationId,
        bool includePermissions = false,
        CancellationToken ct = default
    )
    {
        var query = _context.Roles.Where(r => r.ClientApplicationId == clientApplicationId);

        if (includePermissions)
            query = query.Include(r => r.Permissions);

        return await query.OrderBy(r => r.Name).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Role>> GetUserRolesInApplicationAsync(
        Guid userId,
        Guid clientApplicationId,
        bool includePermissions = true,
        CancellationToken ct = default
    )
    {
        // Paso 1: obtener los IDs de roles del usuario en esta app
        var roleIds = await _context
            .UserApplicationRoles.Where(uar =>
                uar.UserId == userId && uar.ClientApplicationId == clientApplicationId
            )
            .Select(uar => uar.RoleId)
            .ToListAsync(ct);

        if (!roleIds.Any())
            return [];

        // Paso 2: cargar los roles con o sin permisos
        // Include funciona correctamente porque la fuente ahora
        // es DbSet<Role> directamente — no un Select proyectado
        var query = _context.Roles.Where(r => roleIds.Contains(r.Id));

        if (includePermissions)
            query = query.Include(r => r.Permissions);

        return await query.ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Guid>> GetUserApplicationIdsAsync(
        Guid userId,
        CancellationToken ct = default
    ) =>
        await _context
            .UserApplicationRoles.Where(uar => uar.UserId == userId)
            .Select(uar => uar.ClientApplicationId)
            .Distinct()
            .ToListAsync(ct);

    public async Task AddAsync(Role role, CancellationToken ct = default) =>
        await _context.Roles.AddAsync(role, ct);

    public Task UpdateAsync(Role role, CancellationToken ct = default)
    {
        _context.Roles.Update(role);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var role = await _context.Roles.FindAsync([id], ct);
        if (role is not null)
            _context.Roles.Remove(role);
    }

    public async Task AssignRoleToUserAsync(
        UserApplicationRole userApplicationRole,
        CancellationToken ct = default
    ) => await _context.UserApplicationRoles.AddAsync(userApplicationRole, ct);

    public async Task RevokeRoleFromUserAsync(
        Guid userId,
        Guid clientApplicationId,
        Guid roleId,
        CancellationToken ct = default
    )
    {
        var record = await _context.UserApplicationRoles.FirstOrDefaultAsync(
            r =>
                r.UserId == userId
                && r.ClientApplicationId == clientApplicationId
                && r.RoleId == roleId,
            ct
        );

        if (record is not null)
            _context.UserApplicationRoles.Remove(record);
    }

    public async Task<bool> UserHasRoleAsync(
        Guid userId,
        Guid clientApplicationId,
        Guid roleId,
        CancellationToken ct = default
    ) =>
        await _context.UserApplicationRoles.AnyAsync(
            r =>
                r.UserId == userId
                && r.ClientApplicationId == clientApplicationId
                && r.RoleId == roleId,
            ct
        );
}
