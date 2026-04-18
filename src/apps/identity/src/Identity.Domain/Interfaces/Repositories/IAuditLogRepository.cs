using Identity.Domain.Entities;

namespace Identity.Domain.Interfaces.Repositories;

/// <summary>
/// Contrato para el audit log.
///
/// Solo append — nunca update ni delete.
/// AddAsync se llama en cada evento de seguridad del sistema.
/// </summary>
public interface IAuditLogRepository
{
    Task AddAsync(AuditLog auditLog, CancellationToken ct = default);

    Task<(IReadOnlyList<AuditLog> Logs, int TotalCount)> GetByUserAsync(
        Guid userId,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken ct = default
    );

    /// <summary>
    /// eventType null → retorna todos los tipos.
    /// from/to null → sin filtro de fechas.
    /// </summary>
    Task<(IReadOnlyList<AuditLog> Logs, int TotalCount)> GetByTenantAsync(
        Guid tenantId,
        DateTime? from = null,
        DateTime? to = null,
        string? eventType = null,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken ct = default
    );

    /// <summary>
    /// Cuenta intentos fallidos desde una IP en un período.
    /// Se usa para el rate limiting por IP:
    /// "¿cuántos intentos fallidos desde esta IP en los últimos 15 min?"
    /// </summary>
    Task<int> CountFailedLoginsFromIpAsync(
        string ipAddress,
        DateTime since,
        CancellationToken ct = default
    );
}
