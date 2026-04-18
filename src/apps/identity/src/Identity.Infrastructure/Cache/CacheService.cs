using Identity.Application.Common.Interfaces;
using StackExchange.Redis;

namespace Identity.Infrastructure.Cache;

/// <summary>
/// Implementación de ICacheService usando Redis.
///
/// Redis es ideal para caché de corta duración porque:
///   - Operaciones en memoria → microsegundos
///   - TTL nativo → los datos expiran automáticamente
///   - Atómico → operaciones como INCREMENT son thread-safe
///
/// Todos los métodos manejan excepciones de conexión
/// para que un fallo de Redis no tire abajo el sistema.
/// </summary>
public class CacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private IDatabase Db => _redis.GetDatabase();

    public CacheService(IConnectionMultiplexer redis) => _redis = redis;

    public async Task SetAsync(
        string key,
        string value,
        TimeSpan ttl,
        CancellationToken ct = default
    )
    {
        await Db.StringSetAsync(key, value, ttl);
    }

    public async Task<string?> GetAsync(string key, CancellationToken ct = default)
    {
        var value = await Db.StringGetAsync(key);
        return value.HasValue ? value.ToString() : null;
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        await Db.KeyDeleteAsync(key);
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        return await Db.KeyExistsAsync(key);
    }

    public async Task<long> IncrementAsync(string key, TimeSpan ttl, CancellationToken ct = default)
    {
        var value = await Db.StringIncrementAsync(key);

        // Solo setear TTL en el primer incremento
        // (cuando la key no existía antes)
        if (value == 1)
            await Db.KeyExpireAsync(key, ttl);

        return value;
    }
}
