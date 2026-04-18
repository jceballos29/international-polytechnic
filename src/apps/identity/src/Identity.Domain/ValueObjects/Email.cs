using System.Text.RegularExpressions;

namespace Identity.Domain.ValueObjects;

/// <summary>
/// Dirección de email válida y normalizada.
///
/// Garantías que ofrece este tipo:
///   - No es null ni vacío
///   - Tiene formato válido (contiene @ y dominio)
///   - Está en minúsculas (normalizado)
///   - Sin espacios al inicio ni al final
///
/// Si tienes una instancia de Email, SABES que es válida.
/// No necesitas volver a validar en cada lugar donde lo uses.
///
/// Email.Create("Juan@Test.COM") → email.Value = "juan@test.com"
/// Email.Create("esto-no-es-email") → lanza ArgumentException
/// </summary>
public sealed class Email : ValueObject
{
    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    public string Value { get; }

    // Constructor privado — solo se puede crear via Create()
    private Email(string value) => Value = value;

    public static Email Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("El email no puede estar vacío.", nameof(value));

        var normalized = value.Trim().ToLowerInvariant();

        if (normalized.Length > 254)
            throw new ArgumentException(
                "El email no puede tener más de 254 caracteres.",
                nameof(value)
            );

        if (!EmailRegex.IsMatch(normalized))
            throw new ArgumentException(
                $"'{value}' no es una dirección de email válida.",
                nameof(value)
            );

        return new Email(normalized);
    }

    // Define qué propiedades participan en la igualdad
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    // Permite usar Email directamente en strings
    // Console.WriteLine($"Usuario: {email}") → "Usuario: juan@test.com"
    public override string ToString() => Value;

    public string Domain => Value.Split('@')[1];
    public string LocalPart => Value.Split('@')[0];
}
