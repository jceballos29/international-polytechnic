using Identity.Domain.ValueObjects;

namespace Identity.Domain.Entities;

/// <summary>
/// Persona que puede autenticarse en el sistema.
///
/// IMPORTANTE: User pertenece al IdP — es el usuario del sistema
/// de autenticación. Cada app cliente (Universitas, Gradus) tendrá
/// sus propias entidades (Estudiante, Docente) que referencian
/// este UserId de forma lógica, pero sin FK física entre DBs.
///
/// Lo que guarda:
///   - Credenciales (email + password hash)
///   - Nombre (para mostrarlo en el IdP y en los tokens)
///   - Estado de la cuenta (activo, bloqueado)
///   - Historial de intentos fallidos
///   - Sus roles por aplicación
/// </summary>
public class User : BaseEntity
{
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Value Object Email garantiza: formato válido,
    /// minúsculas, sin espacios. Si existe, es válido.
    /// </summary>
    public Email Email { get; private set; } = null!;

    /// <summary>
    /// Hash BCrypt — nunca texto plano, nunca MD5/SHA256.
    /// BCrypt es lento deliberadamente → fuerza bruta imposible.
    /// El Domain no hashea — recibe el hash ya procesado
    /// desde Infrastructure (HashService).
    /// </summary>
    public string PasswordHash { get; private set; } = string.Empty;

    /// <summary>
    /// Nombre completo — opcional al crear la cuenta.
    /// Si no tiene nombre, se usa el email como fallback.
    /// </summary>
    public PersonName? Name { get; private set; }

    public bool IsActive { get; private set; }

    /// <summary>
    /// Intentos fallidos consecutivos.
    /// Se resetea a 0 con cada login exitoso.
    /// Al llegar a 5 → cuenta bloqueada 15 minutos.
    /// </summary>
    public int FailedLoginAttempts { get; private set; }

    /// <summary>
    /// Null = no está bloqueado.
    /// El bloqueo expira automáticamente — no requiere
    /// intervención manual de un admin.
    /// </summary>
    public DateTime? LockedUntil { get; private set; }

    // Lista privada — EF Core puede cargarla,
    // pero nadie externo puede modificarla directamente
    private readonly List<UserApplicationRole> _applicationRoles = new();
    public IReadOnlyCollection<UserApplicationRole> ApplicationRoles =>
        _applicationRoles.AsReadOnly();

    private User() { }

    /// <param name="email">Value Object ya validado.</param>
    /// <param name="passwordHash">Hash BCrypt — viene de HashService.</param>
    /// <param name="name">Opcional al crear la cuenta.</param>
    public static User Create(
        Guid tenantId,
        Email email,
        string passwordHash,
        PersonName? name = null
    )
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("El TenantId es requerido.", nameof(tenantId));

        ArgumentNullException.ThrowIfNull(email, nameof(email));

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException(
                "El hash de contraseña es requerido.",
                nameof(passwordHash)
            );

        return new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = email,
            PasswordHash = passwordHash,
            Name = name,
            IsActive = true,
            FailedLoginAttempts = 0,
            CreatedAt = DateTime.UtcNow,
        };
    }

    // ── Comportamiento de autenticación ───────────────────

    /// <summary>
    /// Registra un intento fallido.
    /// Al llegar a 5 → bloquea 15 minutos.
    /// </summary>
    public void RegisterFailedLogin()
    {
        FailedLoginAttempts++;
        if (FailedLoginAttempts >= 5)
            LockedUntil = DateTime.UtcNow.AddMinutes(15);
        MarkAsUpdated();
    }

    /// <summary>
    /// Login exitoso → resetea todo.
    /// </summary>
    public void RegisterSuccessfulLogin()
    {
        FailedLoginAttempts = 0;
        LockedUntil = null;
        MarkAsUpdated();
    }

    public bool IsLocked() => LockedUntil.HasValue && LockedUntil.Value > DateTime.UtcNow;

    public TimeSpan? LockTimeRemaining() =>
        IsLocked() ? LockedUntil!.Value - DateTime.UtcNow : null;

    // ── Comportamiento de perfil ──────────────────────────

    public void UpdatePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new ArgumentException("El nuevo hash es requerido.", nameof(newPasswordHash));
        PasswordHash = newPasswordHash;
        MarkAsUpdated();
    }

    public void UpdateName(PersonName name)
    {
        ArgumentNullException.ThrowIfNull(name, nameof(name));
        Name = name;
        MarkAsUpdated();
    }

    public void UpdateEmail(Email newEmail)
    {
        ArgumentNullException.ThrowIfNull(newEmail, nameof(newEmail));
        Email = newEmail;
        MarkAsUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkAsUpdated();
    }

    public void Activate()
    {
        IsActive = true;
        FailedLoginAttempts = 0;
        LockedUntil = null;
        MarkAsUpdated();
    }

    // ── Propiedades calculadas ────────────────────────────

    /// <summary>
    /// "Juan García" si tiene nombre, "juan@test.com" si no.
    /// Usar siempre esta propiedad en UI — nunca Name?.DisplayName directamente.
    /// </summary>
    public string DisplayName => Name?.DisplayName ?? Email.Value;

    public string FullName => Name?.FullName ?? Email.Value;

    /// <summary>"JG" si tiene nombre, "J" si no.</summary>
    public string Initials => Name?.Initials ?? Email.Value[0].ToString().ToUpperInvariant();
}
