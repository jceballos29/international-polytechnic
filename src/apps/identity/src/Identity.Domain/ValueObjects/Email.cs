using System.Text.RegularExpressions;

namespace Identity.Domain.ValueObjects;

public sealed class Email : ValueObject
{
    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled
    );

    public string Value { get; }

    private Email(string value) => Value = value;

    public static Email Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email cannot be null or empty.", nameof(value));

        var normalized = value.Trim().ToLowerInvariant();

        if (normalized.Length > 254)
            throw new ArgumentException(
                "Email cannot be longer than 254 characters.",
                nameof(value)
            );

        if (!EmailRegex.IsMatch(normalized))
            throw new ArgumentException($"'{value}' is not a valid email address.", nameof(value));

        return new Email(normalized);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public string Domain() => Value.Split('@')[1];

    public string LocalPart() => Value.Split('@')[0];
}
