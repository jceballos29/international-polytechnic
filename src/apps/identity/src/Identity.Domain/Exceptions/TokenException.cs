namespace Identity.Domain.Exceptions;

/// <summary>
/// Errores relacionados con tokens.
/// Factory methods para cada caso específico.
/// </summary>
public class TokenException : DomainException
{
    protected TokenException(string message, string errorCode, int httpStatusCode = 401)
        : base(message, errorCode, httpStatusCode) { }

    public static TokenException Expired() => new("El token ha expirado.", "TOKEN_EXPIRED");

    public static TokenException Revoked() => new("El token ha sido revocado.", "TOKEN_REVOKED");

    public static TokenException InvalidSignature() =>
        new("La firma del token no es válida.", "TOKEN_INVALID_SIGNATURE");

    public static TokenException MalFormed() =>
        new("El token tiene un formato inválido.", "TOKEN_MALFORMED");

    public static TokenException CodeAlreadyUsed() =>
        new("El authorization code ya fue utilizado.", "CODE_ALREADY_USED", 400);

    public static TokenException PkceFailed() =>
        new("La verificación PKCE falló.", "PKCE_VERIFICATION_FAILED", 400);
}
