using System.Text.RegularExpressions;

namespace Identity.Domain.ValueObjects;

public class ClientId : ValueObject
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
            throw new ArgumentException("ClientId cannot be null or empty.", nameof(value));

        var normalized = value.Trim().ToLowerInvariant();

        if (normalized.Length < 3)
            throw new ArgumentException(
                "ClientId must be at least 3 characters long.",
                nameof(value)
            );

        if (normalized.Length > 100)
            throw new ArgumentException(
                "ClientId cannot be longer than 100 characters.",
                nameof(value)
            );

        if (!ClientIdRegex.IsMatch(normalized))
            throw new ArgumentException(
                $"'{value}' is not a valid ClientId. It must start with a letter, can contain lowercase letters, numbers, and hyphens, and must end with a letter or number."
                    + $" Examples: 'portal', 'gradus-api', 'a1b2c3'.",
                nameof(value)
            );

        return new ClientId(normalized);
    }

    public static ClientId GenerateFrom(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be null or empty.", nameof(name));

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
