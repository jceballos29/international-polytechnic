using Identity.Domain.Entities;
using Identity.Domain.ValueObjects;

namespace Identity.Domain.Interfaces.Repositories;

/// <summary>
/// Contrato para persistencia y recuperación de usuarios.
///
/// Solo declaramos los métodos que realmente necesitan
/// los casos de uso actuales — no métodos "por si acaso".
/// </summary>
public interface IUserRepository
{
    // ── Queries ────────────────────────────────────────────

    /// <summary>
    /// Retorna null si no existe — no lanza excepción.
    /// El Handler decide qué hacer cuando no lo encuentra.
    /// </summary>
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Busca por email dentro de un tenant.
    /// Recibe Value Object Email — ya validado y normalizado.
    /// </summary>
    Task<User?> GetByEmailAsync(Guid tenantId, Email email, CancellationToken ct = default);

    /// <summary>
    /// Lista con paginación obligatoria.
    /// Sin paginación un tenant con miles de usuarios
    /// cargaría toda la tabla en memoria.
    /// pageNumber base 1 — la primera página es 1, no 0.
    /// </summary>
    Task<(IReadOnlyList<User> Users, int TotalCount)> GetAllAsync(
        Guid tenantId,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken ct = default
    );

    /// <summary>
    /// SELECT EXISTS — más eficiente que cargar toda la entidad
    /// solo para verificar si existe. Se usa al registrar
    /// un nuevo usuario para validar email único.
    /// </summary>
    Task<bool> ExistsWithEmailAsync(Guid tenantId, Email email, CancellationToken ct = default);

    /// <summary>
    /// Obtiene el TenantId por dominio.
    /// Usado cuando no hay contexto OAuth (login directo al dashboard).
    /// </summary>
    Task<Guid?> GetTenantIdByDomainAsync(string domain, CancellationToken ct = default);

    // ── Commands ───────────────────────────────────────────

    Task AddAsync(User user, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);
}
