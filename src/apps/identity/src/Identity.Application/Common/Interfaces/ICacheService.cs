namespace Identity.Application.Common.Interfaces;

/// <summary>
/// Contrato para caché de corta duración en Redis.
///
/// Se usa para:
///   - Authorization codes (TTL: 2 minutos)
///   - JWKS cache (TTL: 1 hora)
///   - Token blocklist para revocación (TTL: tiempo restante del token)
///   - Rate limiting (TTL: 15 minutos)
/// </summary>
public interface ICacheService
{
    Task SetAsync(string key, string value, TimeSpan ttl, CancellationToken ct = default);

    Task<string?> GetAsync(string key, CancellationToken ct = default);

    Task DeleteAsync(string key, CancellationToken ct = default);

    Task<bool> ExistsAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Incrementa un contador atómicamente.
    /// Retorna el nuevo valor después del incremento.
    /// Se usa para rate limiting: "cuántos intentos desde esta IP"
    /// </summary>
    Task<long> IncrementAsync(string key, TimeSpan ttl, CancellationToken ct = default);
}
