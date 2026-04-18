namespace Identity.Domain.Entities;

/// <summary>
/// Rol perteneciente a una aplicación cliente específica.
///
/// Los roles son específicos de cada app:
///   "docente" en Universitas ≠ "docente" en Gradus
///   Aunque tengan el mismo nombre, son roles distintos
///   de apps distintas.
///
/// Ejemplos:
///   App "universitas-ui" → roles: rector, decano, docente, estudiante
///   App "gradus-ui"      → roles: estudiante, docente, coordinador
///   App "admin-panel"    → roles: super_admin, tenant_admin
/// </summary>
public class Role : BaseEntity
{
    public Guid TenantId { get; private set; }
    public Guid ClientApplicationId { get; private set; }

    /// <summary>
    /// Ej: "docente", "estudiante", "super_admin"
    /// Único dentro de una misma app.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    private readonly List<Permission> _permissions = new();
    public IReadOnlyCollection<Permission> Permissions => _permissions.AsReadOnly();

    // Navegación para EF Core
    public ClientApplication? ClientApplication { get; private set; }

    private Role() { }

    public static Role Create(
        Guid tenantId,
        Guid clientApplicationId,
        string name,
        string? description = null
    )
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId es requerido.", nameof(tenantId));

        if (clientApplicationId == Guid.Empty)
            throw new ArgumentException(
                "ClientApplicationId es requerido.",
                nameof(clientApplicationId)
            );

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre del rol es requerido.", nameof(name));

        return new Role
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ClientApplicationId = clientApplicationId,
            Name = name.Trim().ToLowerInvariant(),
            Description = description?.Trim(),
            CreatedAt = DateTime.UtcNow,
        };
    }

    public void Update(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre es requerido.", nameof(name));
        Name = name.Trim().ToLowerInvariant();
        Description = description?.Trim();
        MarkAsUpdated();
    }
}
