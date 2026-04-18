using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Domain.Interfaces.Repositories;
using Identity.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistence.Repositories;

public class ClientApplicationRepository : IClientApplicationRepository
{
    private readonly IdentityDbContext _context;

    public ClientApplicationRepository(IdentityDbContext context) => _context = context;

    public async Task<ClientApplication?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _context.ClientApplications.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<ClientApplication?> GetByClientIdAsync(
        ClientId clientId,
        CancellationToken ct = default
    ) =>
        await _context.ClientApplications.FirstOrDefaultAsync(
            a => a.ClientId == clientId && a.IsActive,
            ct
        );

    public async Task<(IReadOnlyList<ClientApplication> Applications, int TotalCount)> GetAllAsync(
        Guid tenantId,
        ApplicationStatus? status = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken ct = default
    )
    {
        var query = _context.ClientApplications.Where(a => a.TenantId == tenantId);

        if (status.HasValue)
        {
            var isActive = status == ApplicationStatus.Active;
            query = query.Where(a => a.IsActive == isActive);
        }

        var totalCount = await query.CountAsync(ct);
        var apps = await query
            .OrderBy(a => a.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (apps, totalCount);
    }

    public async Task<bool> ExistsWithClientIdAsync(
        ClientId clientId,
        CancellationToken ct = default
    ) => await _context.ClientApplications.AnyAsync(a => a.ClientId == clientId, ct);

    public async Task AddAsync(ClientApplication application, CancellationToken ct = default) =>
        await _context.ClientApplications.AddAsync(application, ct);

    public Task UpdateAsync(ClientApplication application, CancellationToken ct = default)
    {
        _context.ClientApplications.Update(application);
        return Task.CompletedTask;
    }
}
