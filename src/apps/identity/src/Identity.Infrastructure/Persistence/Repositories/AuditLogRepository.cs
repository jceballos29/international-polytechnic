using Identity.Domain.Entities;
using Identity.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistence.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly IdentityDbContext _context;

    public AuditLogRepository(IdentityDbContext context) => _context = context;

    public async Task AddAsync(AuditLog auditLog, CancellationToken ct = default) =>
        await _context.AuditLogs.AddAsync(auditLog, ct);

    public async Task<(IReadOnlyList<AuditLog> Logs, int TotalCount)> GetByUserAsync(
        Guid userId,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken ct = default
    )
    {
        var query = _context
            .AuditLogs.Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt);

        var totalCount = await query.CountAsync(ct);
        var logs = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return (logs, totalCount);
    }

    public async Task<(IReadOnlyList<AuditLog> Logs, int TotalCount)> GetByTenantAsync(
        Guid tenantId,
        DateTime? from = null,
        DateTime? to = null,
        string? eventType = null,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken ct = default
    )
    {
        var query = _context.AuditLogs.Where(a => a.TenantId == tenantId);

        if (from.HasValue)
            query = query.Where(a => a.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(a => a.CreatedAt <= to.Value);

        if (!string.IsNullOrWhiteSpace(eventType))
            query = query.Where(a => a.EventType == eventType);

        var totalCount = await query.CountAsync(ct);
        var logs = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (logs, totalCount);
    }

    public async Task<int> CountFailedLoginsFromIpAsync(
        string ipAddress,
        DateTime since,
        CancellationToken ct = default
    ) =>
        await _context.AuditLogs.CountAsync(
            a =>
                a.IpAddress == ipAddress
                && a.EventType == "USER_LOGIN"
                && !a.Success
                && a.CreatedAt >= since,
            ct
        );
}
