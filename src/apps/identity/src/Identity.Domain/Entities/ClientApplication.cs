using Identity.Domain.ValueObjects;

namespace Identity.Domain.Entities;

/// <summary>
/// Aplicación cliente registrada en el IdP.
///
/// Cada app que quiera usar el SSO debe estar registrada aquí.
/// El registro define quién es, cómo se autentica y qué puede hacer.
///
/// Ejemplos registrados:
///   "portal"          → authorization_code + refresh_token
///   "universitas-ui"  → authorization_code + refresh_token
///   "gradus-ui"       → authorization_code + refresh_token
///   "gradus-api"      → client_credentials (M2M)
///   "admin-panel"     → authorization_code + refresh_token
/// </summary>
public class ClientApplication : BaseEntity
{
    public Guid TenantId { get; private set; }

    /// <summary>Nombre visible. Ej: "Portal Estudiantil"</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Value Object ClientId — identificador público de la app.
    /// Viaja en las URLs de OAuth. NO es secreto.
    /// Garantiza formato kebab-case válido.
    /// </summary>
    public ClientId ClientId { get; private set; } = null!;

    /// <summary>
    /// Hash BCrypt del client_secret.
    /// El secret real se muestra UNA SOLA VEZ al registrar la app.
    /// Después solo existe este hash.
    /// Nullable — las apps SPA públicas no tienen secret.
    /// </summary>
    public string? ClientSecretHash { get; private set; }

    /// <summary>
    /// Value Objects RedirectUri — URIs de redirección permitidas.
    /// Validación crítica de seguridad OAuth.
    /// Guardado en PostgreSQL como TEXT[].
    /// </summary>
    public List<RedirectUri> RedirectUris { get; private set; } = new();

    /// <summary>Scopes que esta app puede solicitar.</summary>
    public List<string> AllowedScopes { get; private set; } = new();

    /// <summary>Flujos OAuth permitidos para esta app.</summary>
    public List<string> GrantTypes { get; private set; } = new();

    public string? Description { get; private set; }
    public string? LogoUrl { get; private set; }
    public bool IsActive { get; private set; }

    private readonly List<Role> _roles = new();
    public IReadOnlyCollection<Role> Roles => _roles.AsReadOnly();

    private ClientApplication() { }

    /// <param name="clientId">String — se convierte a Value Object internamente.</param>
    /// <param name="redirectUris">Strings — se convierten a Value Objects internamente.</param>
    /// <param name="clientSecretHash">Hash BCrypt — viene de HashService.</param>
    /// <param name="isDevelopment">Permite URIs HTTP en hosts no-localhost.</param>
    public static ClientApplication Create(
        Guid tenantId,
        string name,
        string clientId,
        List<string> redirectUris,
        List<string> allowedScopes,
        List<string> grantTypes,
        string? clientSecretHash = null,
        string? description = null,
        bool isDevelopment = false
    )
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId es requerido.", nameof(tenantId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre es requerido.", nameof(name));

        // Valida formato: kebab-case, 3-100 chars
        var clientIdVO = ClientId.Create(clientId);

        // Valida cada URI — lanza ArgumentException si alguna es inválida
        var redirectUriVOs = redirectUris
            .Select(uri => RedirectUri.Create(uri, isDevelopment))
            .ToList();

        if (
            redirectUriVOs.Count == 0
            && grantTypes.Contains("authorization_code", StringComparer.OrdinalIgnoreCase)
        )
            throw new ArgumentException(
                "Las apps con authorization_code necesitan " + "al menos una redirect_uri.",
                nameof(redirectUris)
            );

        return new ClientApplication
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name.Trim(),
            ClientId = clientIdVO,
            ClientSecretHash = clientSecretHash,
            RedirectUris = redirectUriVOs,
            AllowedScopes = allowedScopes,
            GrantTypes = grantTypes,
            Description = description?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
    }

    // ── Métodos de consulta ───────────────────────────────

    /// <summary>
    /// Comparación exacta — sin wildcards, sin trailing slashes.
    /// OAuth exige comparación exacta.
    /// </summary>
    public bool IsRedirectUriAllowed(string requestedUri) =>
        RedirectUris.Any(uri => uri.Matches(requestedUri));

    public bool IsGrantTypeAllowed(string grantType) =>
        GrantTypes.Contains(grantType, StringComparer.OrdinalIgnoreCase);

    public bool AreScopesAllowed(IEnumerable<string> requestedScopes) =>
        requestedScopes.All(s => AllowedScopes.Contains(s, StringComparer.OrdinalIgnoreCase));

    // ── Métodos de modificación ───────────────────────────

    public void Update(string name, string? description, string? logoUrl)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre es requerido.", nameof(name));
        Name = name.Trim();
        Description = description?.Trim();
        LogoUrl = logoUrl?.Trim();
        MarkAsUpdated();
    }

    public void UpdateRedirectUris(List<string> redirectUris, bool isDevelopment = false)
    {
        var uriVOs = redirectUris.Select(uri => RedirectUri.Create(uri, isDevelopment)).ToList();

        if (
            uriVOs.Count == 0
            && GrantTypes.Contains("authorization_code", StringComparer.OrdinalIgnoreCase)
        )
            throw new ArgumentException(
                "Las apps con authorization_code necesitan " + "al menos una redirect_uri.",
                nameof(redirectUris)
            );

        RedirectUris = uriVOs;
        MarkAsUpdated();
    }

    /// <summary>El nuevo hash viene de HashService en Infrastructure.</summary>
    public void RotateSecret(string newSecretHash)
    {
        if (string.IsNullOrWhiteSpace(newSecretHash))
            throw new ArgumentException("El nuevo hash es requerido.", nameof(newSecretHash));
        ClientSecretHash = newSecretHash;
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
        MarkAsUpdated();
    }
}
