namespace Identity.Domain.Exceptions;

/// <summary>
/// El usuario autenticado no tiene permiso para esta operación.
///
/// Diferencia clave:
///   401 Unauthorized → no está autenticado (sin token o token inválido)
///   403 Forbidden    → está autenticado pero sin el rol/permiso necesario
///
/// Esta excepción es para el caso 403.
/// </summary>
public class UnauthorizedOperationException : DomainException
{
    public UnauthorizedOperationException(string message)
        : base(message, "FORBIDDEN", 403) { }

    public static UnauthorizedOperationException RequiresRole(string role) =>
        new($"Esta operación requiere el rol '{role}'.");

    public static UnauthorizedOperationException RequiresScope(string scope) =>
        new($"Esta operación requiere el scope '{scope}'.");
}
