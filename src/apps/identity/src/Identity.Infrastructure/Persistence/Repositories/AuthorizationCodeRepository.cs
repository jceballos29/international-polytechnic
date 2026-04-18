using Identity.Domain.Entities;
using Identity.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistence.Repositories;

public class AuthorizationCodeRepository : IAuthorizationCodeRepository
{
    private readonly IdentityDbContext _context;

    public AuthorizationCodeRepository(IdentityDbContext context) => _context = context;

    public async Task<AuthorizationCode?> GetByCodeHashAsync(
        string codeHash,
        CancellationToken ct = default
    ) => await _context.AuthorizationCodes.FirstOrDefaultAsync(c => c.CodeHash == codeHash, ct);

    public async Task AddAsync(AuthorizationCode code, CancellationToken ct = default) =>
        await _context.AuthorizationCodes.AddAsync(code, ct);

    public async Task<bool> MarkAsUsedAsync(Guid codeId, CancellationToken ct = default)
    {
        // UPDATE atómico con condición WHERE used_at IS NULL
        // Garantiza que dos requests simultáneos no puedan
        // usar el mismo code — solo uno tendrá rows afectadas = 1
        var affected = await _context
            .AuthorizationCodes.Where(c => c.Id == codeId && c.UsedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.UsedAt, DateTime.UtcNow), ct);

        return affected > 0;
    }

    public async Task<int> DeleteExpiredAndUsedAsync(CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow;
        return await _context
            .AuthorizationCodes.Where(c => c.ExpiresAt < cutoff || c.UsedAt != null)
            .ExecuteDeleteAsync(ct);
    }
}
