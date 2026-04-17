namespace Identity.Domain.Enums;

public static class AuditEventType
{
   // ----- Eventos de Usuario -----
   public const string UserLogin = "USER_LOGIN";
    public const string UserLogout = "USER_LOGOUT";
    public const string UserRegistration = "USER_REGISTRATION";
    public const string UserPasswordChange = "USER_PASSWORD_CHANGE";
    public const string UserAccountLocked = "USER_ACCOUNT_LOCKED";
    public const string UserDeactivated = "USER_DEACTIVATED";

    // ----- Eventos de Tokens -----
    public const string TokenIssued = "TOKEN_ISSUED";
    public const string TokenRefreshed = "TOKEN_REFRESHED";
    public const string TokenRevoked = "TOKEN_REVOKED";
    public const string TokenValidationFailed = "TOKEN_VALIDATION_FAILED";

    // ----- Eventos de Flujo OAuth -----
    public const string AuthCodeIssued = "AUTH_CODE_ISSUED";
    public const string AuthCodeExchanged = "AUTH_CODE_EXCHANGED";
    public const string AuthCodeFailed = "AUTH_CODE_FAILED";
    public const string M2MTokenIssued = "M2M_TOKEN_ISSUED";

    // ----- Eventos de Aplicaciones -----
    public const string ApplicationCreated = "APPLICATION_CREATED";
    public const string ApplicationSecretRotated = "APPLICATION_SECRET_ROTATED";
    public const string ApplicationSuspended = "APPLICATION_SUSPENDED";

    // ----- Eventos de Roles y Permisos -----
    public const string RoleAssigned = "ROLE_ASSIGNED";
    public const string RoleRevoked = "ROLE_REVOKED";
}