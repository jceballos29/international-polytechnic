namespace Identity.Domain.Entities;

/// <summary>
/// Permiso granular perteneciente a un Rol.
///
/// Los permisos son más específicos que los roles:
///   Rol "docente" → permisos: "grades:write", "courses:read"
///   Rol "estudiante" → permisos: "grades:read", "courses:read"
///
/// Se incluyen en el JWT:
/// {
///   "roles": ["docente"],
///   "permissions": ["grades:write", "courses:read"]
/// }
///
/// Convención de nombres: "{recurso}:{acción}"
///   "grades:read", "grades:write", "users:admin"
/// </summary>
public class Permission : BaseEntity
{
    public Guid RoleId { get; private set; }

    /// <summary>Formato: "recurso:acción". Ej: "grades:write"</summary>
    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public Role? Role { get; private set; }

    private Permission() { }

    public static Permission Create(Guid roleId, string name, string? description = null)
    {
        if (roleId == Guid.Empty)
            throw new ArgumentException("RoleId es requerido.", nameof(roleId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre del permiso es requerido.", nameof(name));

        if (!name.Contains(':'))
            throw new ArgumentException(
                "El permiso debe tener formato 'recurso:accion'. " + "Ej: 'grades:read'",
                nameof(name)
            );

        return new Permission
        {
            Id = Guid.NewGuid(),
            RoleId = roleId,
            Name = name.Trim().ToLowerInvariant(),
            Description = description?.Trim(),
            CreatedAt = DateTime.UtcNow,
        };
    }
}
