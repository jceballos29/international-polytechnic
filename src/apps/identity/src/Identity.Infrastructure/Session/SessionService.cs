using System.Text.Json;
using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Models;
using StackExchange.Redis;

namespace Identity.Infrastructure.Session;

/// <summary>
/// Implementación de ISessionService usando Redis.
///
/// La sesión SSO es lo que hace posible el Single Sign-On.
/// Se crea cuando el usuario hace login y se destruye en logout.
///
/// Estructura de la key en Redis:
///   session:{sessionId} → JSON con SessionInfo → TTL: 24h
///
/// El sessionId es un Guid aleatorio que viaja en una cookie
/// HttpOnly — nunca en localStorage ni en URLs.
/// </summary>
public class SessionService : ISessionService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly TimeSpan _sessionTtl = TimeSpan.FromHours(24);
    private IDatabase Db => _redis.GetDatabase();

    public SessionService(IConnectionMultiplexer redis) => _redis = redis;

    public async Task<string> CreateSessionAsync(
        Guid userId,
        Guid tenantId,
        string email,
        CancellationToken ct = default
    )
    {
        var sessionId = Guid.NewGuid().ToString("N");
        // "N" → sin guiones → más compacto para la cookie

        var session = new SessionInfo
        {
            UserId = userId,
            TenantId = tenantId,
            Email = email,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(_sessionTtl),
        };

        var key = BuildKey(sessionId);
        var value = JsonSerializer.Serialize(session);

        await Db.StringSetAsync(key, value, _sessionTtl);

        return sessionId;
    }

    public async Task<SessionInfo?> GetSessionAsync(
        string sessionId,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return null;

        var key = BuildKey(sessionId);
        var value = await Db.StringGetAsync(key);

        if (!value.HasValue)
            return null;

        var session = JsonSerializer.Deserialize<SessionInfo>(value.ToString()!);

        if (session is null || session.IsExpired())
        {
            await Db.KeyDeleteAsync(key);
            return null;
        }

        return session;
    }

    public async Task DestroySessionAsync(string sessionId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return;
        await Db.KeyDeleteAsync(BuildKey(sessionId));
    }

    private static string BuildKey(string sessionId) => $"session:{sessionId}";
}
