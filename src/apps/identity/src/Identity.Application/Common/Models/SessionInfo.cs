namespace Identity.Application.Common.Models;

/// <summary>
/// Información de la sesión SSO almacenada en Redis.
///
/// Cuando el usuario hace login en el IdP, se crea una sesión
/// en Redis con estos datos. El sessionId viaja en una cookie
/// HttpOnly en el browser.
///
/// Cuando otra app redirige al IdP:
///   1. IdP lee la cookie → obtiene el sessionId
///   2. IdP busca la sesión en Redis con ese sessionId
///   3. Si existe y no expiró → emite tokens sin mostrar login (SSO)
///   4. Si no existe → muestra la pantalla de login
/// </summary>
public class SessionInfo
{
    public Guid UserId { get; init; }
    public Guid TenantId { get; init; }
    public string Email { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime ExpiresAt { get; init; }

    public bool IsExpired() => DateTime.UtcNow > ExpiresAt;
}
