namespace Identity.Application.Common.Models;

/// <summary>
/// Representa el resultado de una operación — éxito o fallo.
///
/// ¿Por qué no simplemente lanzar excepciones?
/// Las excepciones son para situaciones excepcionales (bugs,
/// errores de infraestructura). Los errores de negocio esperados
/// (credenciales incorrectas, token expirado) no son excepciones
/// — son resultados válidos de una operación.
///
/// Con Result Pattern:
///   - El compilador fuerza al caller a manejar ambos casos
///   - El flujo es más legible y predecible
///   - Los controllers se mantienen delgados y limpios
///
/// Uso:
///   var result = await _mediator.Send(command);
///   if (result.IsSuccess) return Ok(result.Value);
///   return result.ToProblemDetails();
/// </summary>
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }

    private Result(bool isSuccess, T? value, string? errorCode, string? errorMessage)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public static Result<T> Success(T value) => new(true, value, null, null);

    public static Result<T> Failure(string errorCode, string errorMessage) =>
        new(false, default, errorCode, errorMessage);

    /// <summary>
    /// Convierte un Result fallido de tipo T a tipo U.
    /// Útil para propagar errores entre capas.
    /// </summary>
    public Result<U> ToFailure<U>()
    {
        if (IsSuccess)
            throw new InvalidOperationException(
                "No se puede convertir un resultado exitoso a fallo."
            );
        return Result<U>.Failure(ErrorCode!, ErrorMessage!);
    }
}

/// <summary>
/// Versión sin valor de retorno — para operaciones void.
/// Ej: logout, revocar token, asignar rol.
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }

    private Result(bool isSuccess, string? errorCode, string? errorMessage)
    {
        IsSuccess = isSuccess;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public static Result Success() => new(true, null, null);

    public static Result Failure(string errorCode, string errorMessage) =>
        new(false, errorCode, errorMessage);
}
