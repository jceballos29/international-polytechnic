namespace Identity.Domain.Entities;

/// <summary>
/// Relación entre un Usuario, una Aplicación y un Rol.
///
/// Responde: "¿Qué rol tiene el Usuario X dentro de la App Y?"
///
/// Juan en Universitas → rol "docente"
/// Juan en Gradus      → rol "estudiante"
/// Juan en AdminPanel  → rol "super_admin"
///
/// PK compuesta (UserId, ClientApplicationId, RoleId) →
/// garantiza que no se puede asignar el mismo rol dos veces
/// al mismo usuario en la misma app.
/// </summary>
public class UserApplicationRole
{
    public Guid UserId { get; private set; }
    public Guid ClientApplicationId { get; private set; }
    public Guid RoleId { get; private set; }
    public DateTime AssignedAt { get; private set; }

    /// <summary>
    /// UserId del admin que asignó el rol.
    /// Nullable — el seed inicial no tiene admin previo.
    /// </summary>
    public Guid? AssignedBy { get; private set; }

    // Navegaciones para EF Core
    public User? User { get; private set; }
    public ClientApplication? ClientApplication { get; private set; }
    public Role? Role { get; private set; }

    private UserApplicationRole() { }

    public static UserApplicationRole Create(
        Guid userId,
        Guid clientApplicationId,
        Guid roleId,
        Guid? assignedBy = null
    )
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId es requerido.", nameof(userId));

        if (clientApplicationId == Guid.Empty)
            throw new ArgumentException(
                "ClientApplicationId es requerido.",
                nameof(clientApplicationId)
            );

        if (roleId == Guid.Empty)
            throw new ArgumentException("RoleId es requerido.", nameof(roleId));

        return new UserApplicationRole
        {
            UserId = userId,
            ClientApplicationId = clientApplicationId,
            RoleId = roleId,
            AssignedAt = DateTime.UtcNow,
            AssignedBy = assignedBy,
        };
    }
}
