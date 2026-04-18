namespace Identity.Domain.Exceptions;

/// <summary>
/// Las credenciales son incorrectas o la cuenta no puede autenticarse.
///
/// ¿Por qué un solo tipo para email no encontrado, password incorrecto
/// y cuenta bloqueada?
/// Seguridad por oscuridad — si le decimos al atacante exactamente
/// qué falló ("email no existe" vs "password incorrecto"), le damos
/// información para ataques de enumeración de usuarios.
/// Siempre retornamos el mismo mensaje genérico.
///
/// Excepción: si la cuenta está bloqueada, SÍ informamos al usuario
/// legítimo con el tiempo restante — él ya sabe que tiene cuenta.
/// </summary>
public class InvalidCredentialsException : DomainException
{
    public InvalidCredentialsException()
        : base("Las credenciales proporcionadas son incorrectas.", "INVALID_CREDENTIALS", 401) { }

    public InvalidCredentialsException(string message)
        : base(message, "INVALID_CREDENTIALS", 401) { }

    public static InvalidCredentialsException AccountLocked(TimeSpan timeRemaining)
    {
        var minutes = (int)Math.Ceiling(timeRemaining.TotalMinutes);
        return new InvalidCredentialsException(
            $"Cuenta bloqueada por {minutes} minuto(s) " + "debido a múltiples intentos fallidos."
        );
    }
}
