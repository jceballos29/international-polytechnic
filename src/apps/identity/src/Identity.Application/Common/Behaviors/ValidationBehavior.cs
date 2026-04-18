using FluentValidation;
using Identity.Application.Common.Models;
using MediatR;

namespace Identity.Application.Common.Behaviors;

/// <summary>
/// Pipeline behavior que valida automáticamente cada Command/Query
/// antes de que llegue al Handler.
///
/// Funciona con FluentValidation — si existe un Validator para el
/// request, lo ejecuta antes del handler.
///
/// Si hay errores de validación → retorna Result.Failure
/// Si no hay errores → deja pasar al handler
///
/// ¿Por qué retornar Result.Failure en lugar de lanzar excepción?
/// Porque los errores de validación son resultados esperados —
/// no son situaciones excepcionales. El frontend los muestra
/// como mensajes de error al usuario.
/// </summary>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : class
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators) =>
        _validators = validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        // Si no hay validators registrados para este request → pasar
        if (!_validators.Any())
            return await next();

        // Ejecutar todos los validators en paralelo
        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken))
        );

        // Recolectar todos los errores
        var failures = validationResults.SelectMany(r => r.Errors).Where(f => f != null).ToList();

        if (!failures.Any())
            return await next();

        // Construir mensaje de error con todos los fallos
        var errorMessage = string.Join("; ", failures.Select(f => f.ErrorMessage));

        // Retornar Result.Failure si TResponse es Result<T>
        // Esto requiere reflexión porque TResponse es genérico
        var responseType = typeof(TResponse);

        if (
            responseType.IsGenericType
            && responseType.GetGenericTypeDefinition() == typeof(Result<>)
        )
        {
            var resultType = responseType.GetGenericArguments()[0];
            var failureMethod = typeof(Result<>)
                .MakeGenericType(resultType)
                .GetMethod(nameof(Result<object>.Failure), [typeof(string), typeof(string)])!;

            return (TResponse)failureMethod.Invoke(null, ["VALIDATION_ERROR", errorMessage])!;
        }

        if (responseType == typeof(Result))
            return (Result.Failure("VALIDATION_ERROR", errorMessage) as TResponse)!;

        // Si TResponse no es un Result, lanzar excepción de validación
        throw new ValidationException(failures);
    }
}
