namespace Identity.Domain.Exceptions;

/// <summary>
/// La app cliente no existe o no está activa en el IdP.
/// Se lanza cuando el client_id del request OAuth no existe
/// o la app está Inactive o Suspended.
/// </summary>
public class ApplicationNotFoundException : DomainException
{
    public ApplicationNotFoundException(string clientId)
        : base(
            $"La aplicación '{clientId}' no está registrada o no está activa.",
            "APPLICATION_NOT_FOUND",
            401
        ) { }
}
