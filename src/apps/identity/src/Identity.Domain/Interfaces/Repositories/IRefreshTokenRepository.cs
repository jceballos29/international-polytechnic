using Identity.Domain.Entities;

namespace Identity.Domain.Interfaces.Repositories;

/// <summary>
/// Contrato para repositorio de Refresh Tokens.
///
/// En DB guardamos el hash del token, nunca el valor real.
/// Flujo de uso en refresh:
///   1. Llega el token opaco: "abc123..."
///   2. Calculamos SHA256: "d4e5f6..."
///   3. Buscamos en DB por el hash
///   4. Verificamos que no esté revocado ni expirado
///   5. Emitimos nuevo Access Token y rotamos el Refresh Token
/// </summary>
public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default);

    /// <summary>
    /// Sesiones activas del usuario en una app.
    /// Un usuario puede tener múltiples tokens si inicia sesión
    /// desde varios dispositivos.
    /// </summary>
    Task<IReadOnlyList<RefreshToken>> GetActiveByUserAsync(
        Guid userId,
        Guid clientApplicationId,
        CancellationToken ct = default
    );

    Task AddAsync(RefreshToken token, CancellationToken ct = default);
    Task UpdateAsync(RefreshToken token, CancellationToken ct = default);

    /// <summary>
    /// Logout de una app — invalida todos los tokens
    /// del usuario en esa app específica.
    /// </summary>
    Task RevokeAllUserTokensAsync(
        Guid userId,
        Guid clientApplicationId,
        CancellationToken ct = default
    );

    /// <summary>
    /// Logout global — invalida todas las sesiones
    /// del usuario en todas las apps.
    /// Se usa al desactivar una cuenta.
    /// </summary>
    Task RevokeAllUserTokensGlobalAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Limpieza periódica — tokens expirados siguen
    /// ocupando espacio aunque ya no sirvan.
    /// Retorna cantidad eliminada para logs.
    /// </summary>
    Task<int> DeleteExpiredAsync(CancellationToken ct = default);
}
