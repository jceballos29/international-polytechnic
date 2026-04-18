namespace Identity.Domain.Enums;

/// <summary>
/// Tipos de eventos registrados en el audit log.
///
/// ¿Por qué static class con const string en lugar de enum?
/// Los enums en C# se guardan como enteros en la DB por defecto.
/// Para que el audit log sea legible directamente en PostgreSQL
/// sin joins ni lookups, guardamos strings descriptivos.
///
/// Con static class obtenemos el mismo beneficio de autocompletar
/// y evitar typos, pero los valores son strings legibles:
///   "USER_LOGIN" en lugar de 0
///   "TOKEN_ISSUED" en lugar de 5
///
/// Convención: ENTIDAD_ACCION en SCREAMING_SNAKE_CASE
/// </summary>
public static class AuditEventType
{
    // ── Eventos de Usuario ─────────────────────────────────
    public const string UserLogin = "USER_LOGIN";
    public const string UserLogout = "USER_LOGOUT";
    public const string UserRegistered = "USER_REGISTERED";
    public const string UserPasswordChanged = "USER_PASSWORD_CHANGED";
    public const string UserAccountLocked = "USER_ACCOUNT_LOCKED";
    public const string UserDeactivated = "USER_DEACTIVATED";

    // ── Eventos de Tokens ──────────────────────────────────
    public const string TokenIssued = "TOKEN_ISSUED";
    public const string TokenRefreshed = "TOKEN_REFRESHED";
    public const string TokenRevoked = "TOKEN_REVOKED";
    public const string TokenValidationFailed = "TOKEN_VALIDATION_FAILED";

    // ── Eventos de Flujo OAuth ─────────────────────────────
    public const string AuthCodeIssued = "AUTH_CODE_ISSUED";
    public const string AuthCodeExchanged = "AUTH_CODE_EXCHANGED";
    public const string AuthCodeFailed = "AUTH_CODE_FAILED";
    public const string M2MTokenIssued = "M2M_TOKEN_ISSUED";

    // ── Eventos de Aplicaciones ────────────────────────────
    public const string ApplicationCreated = "APPLICATION_CREATED";
    public const string ApplicationSecretRotated = "APPLICATION_SECRET_ROTATED";
    public const string ApplicationSuspended = "APPLICATION_SUSPENDED";

    // ── Eventos de Roles ───────────────────────────────────
    public const string RoleAssigned = "ROLE_ASSIGNED";
    public const string RoleRevoked = "ROLE_REVOKED";
}
