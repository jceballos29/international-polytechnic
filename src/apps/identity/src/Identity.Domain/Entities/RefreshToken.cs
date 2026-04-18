namespace Identity.Domain.Entities;

/// <summary>
/// Token opaco de larga duración para renovar el Access Token.
///
/// ¿Por qué existe?
/// El Access Token expira en 15 minutos para minimizar riesgo
/// si es robado. El Refresh Token permite renovarlo sin que el
/// usuario vuelva a hacer login.
///
/// Rotación (seguridad crítica):
/// Cuando se usa un Refresh Token, el anterior se invalida y se
/// emite uno nuevo. Si alguien roba un token y lo usa, el usuario
/// legítimo detectará que el suyo ya no funciona.
///
/// El campo ReplacedById crea cadena de auditoría:
/// Token A → usó → Token B → usó → Token C (actual)
/// </summary>
public class RefreshToken : BaseEntity
{
    public Guid UserId { get; private set; }
    public Guid ClientApplicationId { get; private set; }

    /// <summary>
    /// Hash SHA256 del token real.
    /// El token real (string aleatorio) solo existe en el response.
    /// En DB guardamos el hash — si alguien accede a la DB,
    /// no puede usar los tokens.
    /// </summary>
    public string TokenHash { get; private set; } = string.Empty;

    public List<string> Scopes { get; private set; } = new();
    public DateTime ExpiresAt { get; private set; }

    /// <summary>Null = sigue vigente.</summary>
    public DateTime? RevokedAt { get; private set; }

    /// <summary>ID del token que reemplazó a este — para trazabilidad.</summary>
    public Guid? ReplacedById { get; private set; }

    public string? IssuedFromIp { get; private set; }

    public User? User { get; private set; }
    public ClientApplication? ClientApplication { get; private set; }

    private RefreshToken() { }

    public static RefreshToken Create(
        Guid userId,
        Guid clientApplicationId,
        string tokenHash,
        List<string> scopes,
        int expiryDays = 30,
        string? issuedFromIp = null
    )
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId es requerido.", nameof(userId));

        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new ArgumentException("El hash del token es requerido.", nameof(tokenHash));

        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ClientApplicationId = clientApplicationId,
            TokenHash = tokenHash,
            Scopes = scopes,
            ExpiresAt = DateTime.UtcNow.AddDays(expiryDays),
            IssuedFromIp = issuedFromIp,
            CreatedAt = DateTime.UtcNow,
        };
    }

    /// <summary>Rota el token — registra por cuál fue reemplazado.</summary>
    public void Revoke(Guid replacedById)
    {
        RevokedAt = DateTime.UtcNow;
        ReplacedById = replacedById;
        MarkAsUpdated();
    }

    /// <summary>Revoca sin reemplazo — logout explícito.</summary>
    public void RevokeWithoutReplacement()
    {
        RevokedAt = DateTime.UtcNow;
        MarkAsUpdated();
    }

    public bool IsActive() => RevokedAt == null && DateTime.UtcNow < ExpiresAt;
}
