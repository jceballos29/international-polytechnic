namespace Identity.Domain.Entities;

/// <summary>
/// Código de autorización temporal del flujo OAuth.
///
/// ¿Por qué existe?
/// El IdP no puede enviar el token directamente en la URL
/// porque sería visible en logs del servidor e historial del browser.
/// En cambio envía un "code" temporal que solo el servidor
/// de la app puede intercambiar por tokens.
///
/// Propiedades de seguridad:
///   - Vida muy corta: 2 minutos
///   - Un solo uso: se marca como usado al intercambiarse
///   - Ligado a client_id y redirect_uri específicos
///   - Protegido por PKCE (code_challenge/code_verifier)
/// </summary>
public class AuthorizationCode : BaseEntity
{
    public Guid UserId { get; private set; }
    public Guid ClientApplicationId { get; private set; }

    /// <summary>Hash SHA256 del code real que viaja en la URL.</summary>
    public string CodeHash { get; private set; } = string.Empty;

    /// <summary>
    /// La redirect_uri exacta del request inicial.
    /// Al intercambiar, la app DEBE enviar la misma URI.
    /// </summary>
    public string RedirectUri { get; private set; } = string.Empty;

    public List<string> Scopes { get; private set; } = new();

    /// <summary>
    /// PKCE: BASE64URL(SHA256(code_verifier))
    /// Protege contra intercepción del code.
    /// </summary>
    public string CodeChallenge { get; private set; } = string.Empty;

    /// <summary>Siempre "S256" — nunca "plain".</summary>
    public string CodeChallengeMethod { get; private set; } = "S256";

    /// <summary>
    /// Protege contra ataques CSRF.
    /// El cliente verifica que el state del callback
    /// coincide con el que envió al inicio.
    /// </summary>
    public string? State { get; private set; }

    public string SessionId { get; private set; } = string.Empty;

    public DateTime ExpiresAt { get; private set; }

    /// <summary>Null = aún no se ha intercambiado.</summary>
    public DateTime? UsedAt { get; private set; }

    public User? User { get; private set; }
    public ClientApplication? ClientApplication { get; private set; }

    private AuthorizationCode() { }

    public static AuthorizationCode Create(
        Guid userId,
        Guid clientApplicationId,
        string codeHash,
        string redirectUri,
        List<string> scopes,
        string codeChallenge,
        string sessionId,
        string? state = null
    )
    {
        if (string.IsNullOrWhiteSpace(codeHash))
            throw new ArgumentException("El hash del code es requerido.", nameof(codeHash));

        if (string.IsNullOrWhiteSpace(redirectUri))
            throw new ArgumentException("La redirect_uri es requerida.", nameof(redirectUri));

        if (string.IsNullOrWhiteSpace(codeChallenge))
            throw new ArgumentException("El code_challenge es requerido.", nameof(codeChallenge));

        return new AuthorizationCode
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ClientApplicationId = clientApplicationId,
            CodeHash = codeHash,
            RedirectUri = redirectUri,
            Scopes = scopes,
            CodeChallenge = codeChallenge,
            SessionId = sessionId,
            CodeChallengeMethod = "S256",
            State = state,
            ExpiresAt = DateTime.UtcNow.AddMinutes(2),
            CreatedAt = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Marca el code como usado — un solo uso.
    /// Si ya fue usado, lanza excepción — posible ataque de replay.
    /// </summary>
    public void MarkAsUsed()
    {
        if (UsedAt.HasValue)
            throw new InvalidOperationException("Este authorization code ya fue utilizado.");
        UsedAt = DateTime.UtcNow;
        MarkAsUpdated();
    }

    public bool IsValid() => UsedAt == null && DateTime.UtcNow < ExpiresAt;
}
