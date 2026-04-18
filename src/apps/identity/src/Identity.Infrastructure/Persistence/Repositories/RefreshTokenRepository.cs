using Identity.Domain.Entities;
using Identity.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistence.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IdentityDbContext _context;

    public RefreshTokenRepository(IdentityDbContext context) => _context = context;

    public async Task<RefreshToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken ct = default
    ) => await _context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task<IReadOnlyList<RefreshToken>> GetActiveByUserAsync(
        Guid userId,
        Guid clientApplicationId,
        CancellationToken ct = default
    ) =>
        await _context
            .RefreshTokens.Where(t =>
                t.UserId == userId
                && t.ClientApplicationId == clientApplicationId
                && t.RevokedAt == null
                && t.ExpiresAt > DateTime.UtcNow
            )
            .ToListAsync(ct);

    public async Task AddAsync(RefreshToken token, CancellationToken ct = default) =>
        await _context.RefreshTokens.AddAsync(token, ct);

    public Task UpdateAsync(RefreshToken token, CancellationToken ct = default)
    {
        _context.RefreshTokens.Update(token);
        return Task.CompletedTask;
    }

    public async Task RevokeAllUserTokensAsync(
        Guid userId,
        Guid clientApplicationId,
        CancellationToken ct = default
    )
    {
        var tokens = await _context
            .RefreshTokens.Where(t =>
                t.UserId == userId
                && t.ClientApplicationId == clientApplicationId
                && t.RevokedAt == null
            )
            .ToListAsync(ct);

        foreach (var token in tokens)
            token.RevokeWithoutReplacement();
    }

    public async Task RevokeAllUserTokensGlobalAsync(Guid userId, CancellationToken ct = default)
    {
        var tokens = await _context
            .RefreshTokens.Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(ct);

        foreach (var token in tokens)
            token.RevokeWithoutReplacement();
    }

    public async Task<int> DeleteExpiredAsync(CancellationToken ct = default)
    {
        var expired = await _context
            .RefreshTokens.Where(t => t.ExpiresAt < DateTime.UtcNow)
            .ToListAsync(ct);

        _context.RefreshTokens.RemoveRange(expired);
        return expired.Count;
    }
}
