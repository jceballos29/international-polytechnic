using Identity.Domain.Entities;

namespace Identity.Domain.Interfaces.Repositories;

/// <summary>
/// Contrato para repositorio de Authorization Codes.
///
/// ¿Por qué PostgreSQL y no solo Redis para los codes?
/// Redis es más rápido, pero PostgreSQL nos da:
///   - Historial permanente (auditoría)
///   - Detección de reuso (ataque de replay)
///   - Transaccionalidad al marcar como usado
/// En la práctica usamos AMBOS:
///   - Redis: verificación rápida en el hot path
///   - PostgreSQL: registro permanente y auditoría
/// </summary>
public interface IAuthorizationCodeRepository
{
    Task<AuthorizationCode?> GetByCodeHashAsync(string codeHash, CancellationToken ct = default);

    Task AddAsync(AuthorizationCode code, CancellationToken ct = default);

    /// <summary>
    /// Marca el code como usado de forma atómica.
    /// La implementación usa UPDATE con WHERE used_at IS NULL
    /// para garantizar que dos requests simultáneos no puedan
    /// usar el mismo code.
    ///
    /// Retorna true → marcado exitosamente.
    /// Retorna false → ya estaba usado → posible ataque de replay.
    /// </summary>
    Task<bool> MarkAsUsedAsync(Guid codeId, CancellationToken ct = default);

    Task<int> DeleteExpiredAndUsedAsync(CancellationToken ct = default);
}
