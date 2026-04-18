namespace Identity.Domain.Entities;

/// <summary>
/// Representa una organización o "inquilino" en el sistema.
///
/// En un sistema multi-tenant, cada tenant tiene sus propios
/// usuarios, aplicaciones y roles — completamente aislados.
///
/// Hoy tenemos un solo tenant "Default", pero todas las
/// entidades incluyen TenantId desde el inicio para evitar
/// una reescritura costosa en el futuro.
/// </summary>
public class Tenant : BaseEntity
{
    /// <summary>Nombre visible. Ej: "International Polytechnic"</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Dominio asociado. Ej: "polisystem.edu.co"
    /// En multi-tenant se usa para identificar el tenant
    /// por subdominio: tenant1.auth.com, tenant2.auth.com
    /// </summary>
    public string Domain { get; private set; } = string.Empty;

    /// <summary>
    /// Si está inactivo, todos los usuarios de este tenant
    /// no pueden autenticarse.
    /// </summary>
    public bool IsActive { get; private set; }

    private Tenant() { }

    public static Tenant Create(string name, string domain)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre del tenant es requerido.", nameof(name));

        if (string.IsNullOrWhiteSpace(domain))
            throw new ArgumentException("El dominio del tenant es requerido.", nameof(domain));

        return new Tenant
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Domain = domain.Trim().ToLowerInvariant(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkAsUpdated();
    }

    public void Activate()
    {
        IsActive = true;
        MarkAsUpdated();
    }
}
