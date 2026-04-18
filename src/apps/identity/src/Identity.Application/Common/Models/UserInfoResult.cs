namespace Identity.Application.Common.Models;

/// <summary>
/// Respuesta del endpoint GET /oauth/userinfo.
///
/// Retorna información del usuario autenticado.
/// El cliente debe tener un Access Token válido con scope "openid".
///
/// Campos estándar OIDC:
///   sub     → Subject — ID único del usuario (nunca cambia)
///   email   → email del usuario
///   name    → nombre para mostrar
///
/// Campos adicionales nuestros:
///   roles       → roles del usuario en la app del token (aud)
///   permissions → permisos de esos roles
///   tenant_id   → tenant al que pertenece
/// </summary>
public class UserInfoResult
{
    public string Sub { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Name { get; init; }
    public string? GivenName { get; init; }
    public string? FamilyName { get; init; }
    public string? Initials { get; init; }
    public string TenantId { get; init; } = string.Empty;
    public IReadOnlyList<string> Roles { get; init; } = [];
    public IReadOnlyList<string> Permissions { get; init; } = [];
}
