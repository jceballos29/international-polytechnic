using System.Text.RegularExpressions;

namespace Identity.Domain.ValueObjects;

/// <summary>
/// Identificador público de una aplicación cliente registrada en el IdP.
///
/// Reglas de formato:
///   - Solo letras minúsculas, números y guiones
///   - Entre 3 y 100 caracteres
///   - Debe empezar con una letra
///   - No puede terminar con guión
///
/// Ejemplos válidos:   "portal", "admin-panel", "gradus-api"
/// Ejemplos inválidos: "Portal" (mayúscula), "mi app" (espacio),
///                     "-portal" (empieza con guión), "app-" (termina con guión)
///
/// ¿Por qué estas restricciones?
/// El ClientId viaja en URLs de OAuth:
///   /oauth/authorize?client_id=portal
/// Caracteres especiales en URLs causan problemas de encoding.
/// </summary>
public sealed class ClientId : ValueObject
{
    private static readonly Regex ClientIdRegex = new(
        @"^[a-z][a-z0-9\-]{1,98}[a-z0-9]$",
        RegexOptions.Compiled
    );

    public string Value { get; }

    private ClientId(string value) => Value = value;

    public static ClientId Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("El ClientId no puede estar vacío.", nameof(value));

        var normalized = value.Trim().ToLowerInvariant();

        if (normalized.Length < 3)
            throw new ArgumentException(
                "El ClientId debe tener al menos 3 caracteres.",
                nameof(value)
            );

        if (normalized.Length > 100)
            throw new ArgumentException(
                "El ClientId no puede tener más de 100 caracteres.",
                nameof(value)
            );

        if (!ClientIdRegex.IsMatch(normalized))
            throw new ArgumentException(
                $"'{value}' no es un ClientId válido. "
                    + "Use solo letras minúsculas, números y guiones. "
                    + "Debe empezar con letra y no terminar con guión. "
                    + "Ejemplo: 'portal', 'gradus-api'",
                nameof(value)
            );

        return new ClientId(normalized);
    }

    /// <summary>
    /// Genera un ClientId sugerido a partir de un nombre legible.
    /// "Portal Estudiantil" → "portal-estudiantil"
    /// Útil al registrar una nueva app desde el admin-panel.
    /// </summary>
    public static ClientId GenerateFrom(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre no puede estar vacío.", nameof(name));

        var generated = name.Trim()
            .ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("_", "-")
            .Where(c => char.IsLetterOrDigit(c) || c == '-')
            .Aggregate(string.Empty, (acc, c) => acc + c)
            .Replace("--", "-")
            .Trim('-');

        return Create(generated);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
