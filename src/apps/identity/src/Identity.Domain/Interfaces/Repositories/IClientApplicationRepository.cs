using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Domain.ValueObjects;

namespace Identity.Domain.Interfaces.Repositories;

/// <summary>
/// Contrato para persistencia y recuperación de aplicaciones cliente.
///
/// GetByClientIdAsync es el método más usado del sistema —
/// se llama en CADA request OAuth para validar que la app existe.
/// La DB debe tener índice en la columna client_id.
/// </summary>
public interface IClientApplicationRepository
{
    // ── Queries ────────────────────────────────────────────

    Task<ClientApplication?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Búsqueda por ClientId público — hot path del sistema.
    /// Se ejecuta en cada request de autenticación.
    /// </summary>
    Task<ClientApplication?> GetByClientIdAsync(ClientId clientId, CancellationToken ct = default);

    /// <summary>
    /// status null → retorna todas sin filtrar por estado.
    /// </summary>
    Task<(IReadOnlyList<ClientApplication> Applications, int TotalCount)> GetAllAsync(
        Guid tenantId,
        ApplicationStatus? status = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken ct = default
    );

    Task<bool> ExistsWithClientIdAsync(ClientId clientId, CancellationToken ct = default);

    // ── Commands ───────────────────────────────────────────

    Task AddAsync(ClientApplication application, CancellationToken ct = default);
    Task UpdateAsync(ClientApplication application, CancellationToken ct = default);
}
