namespace Identity.Domain.Exceptions;

/// <summary>
/// Clase base para todas las excepciones del dominio.
///
/// ErrorCode → string estandarizado que el frontend interpreta.
///   Formato: SCREAMING_SNAKE_CASE
///   Ej: "INVALID_CREDENTIALS", "TOKEN_EXPIRED"
///
/// HttpStatusCode → código HTTP que la API retornará.
///   La API no necesita saber el código — lo lee de aquí.
/// </summary>
public abstract class DomainException : Exception
{
    public string ErrorCode { get; }
    public int HttpStatusCode { get; }

    protected DomainException(string message, string errorCode, int httpStatusCode = 400)
        : base(message)
    {
        ErrorCode = errorCode;
        HttpStatusCode = httpStatusCode;
    }
}
