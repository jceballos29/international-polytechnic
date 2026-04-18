using Identity.Domain.Entities;

namespace Identity.Domain.Interfaces.Repositories;

/// <summary>
/// Contrato para repositorio de roles.
///
/// GetUserRolesInApplicationAsync es el método crítico —
/// se llama al generar el JWT para incluir los roles
/// y permisos del usuario en el token.
/// </summary>
public interface IRoleRepository
{
    // ── Queries ────────────────────────────────────────────

    Task<Role?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<Role?> GetByNameAsync(
        Guid clientApplicationId,
        string name,
        CancellationToken ct = default
    );

    /// <summary>
    /// includePermissions = true → carga roles Y permisos en una sola query.
    /// Se usa al generar el JWT.
    /// includePermissions = false → más eficiente para listar en admin-panel.
    /// </summary>
    Task<IReadOnlyList<Role>> GetByApplicationAsync(
        Guid clientApplicationId,
        bool includePermissions = false,
        CancellationToken ct = default
    );

    /// <summary>
    /// Carga los roles y permisos del usuario X en la app Y.
    /// Resultado va directo al JWT:
    /// { "roles": ["docente"], "permissions": ["grades:write"] }
    /// </summary>
    Task<IReadOnlyList<Role>> GetUserRolesInApplicationAsync(
        Guid userId,
        Guid clientApplicationId,
        bool includePermissions = true,
        CancellationToken ct = default
    );

    /// <summary>
    /// Retorna los IDs de las apps donde el usuario tiene al menos un rol.
    /// Se usa en el dashboard para mostrar las apps habilitadas.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetUserApplicationIdsAsync(
        Guid userId,
        CancellationToken ct = default
    );

    // ── Commands ───────────────────────────────────────────

    Task AddAsync(Role role, CancellationToken ct = default);
    Task UpdateAsync(Role role, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    // ── Asignación de roles ────────────────────────────────

    Task AssignRoleToUserAsync(
        UserApplicationRole userApplicationRole,
        CancellationToken ct = default
    );

    Task RevokeRoleFromUserAsync(
        Guid userId,
        Guid clientApplicationId,
        Guid roleId,
        CancellationToken ct = default
    );

    Task<bool> UserHasRoleAsync(
        Guid userId,
        Guid clientApplicationId,
        Guid roleId,
        CancellationToken ct = default
    );
}
