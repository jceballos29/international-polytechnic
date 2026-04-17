namespace Identity.Domain.ValueObjects;

public sealed class PersonName : ValueObject
{
    public string FirstName { get; }
    public string? MiddleName { get; }
    public string FirstLastName { get; }
    public string? SecondLastName { get; }

    private PersonName(
        string firstName,
        string? middleName,
        string firstLastName,
        string? secondLastName
    )
    {
        FirstName = firstName;
        MiddleName = middleName;
        FirstLastName = firstLastName;
        SecondLastName = secondLastName;
    }

    public static PersonName Create(
        string firstName,
        string firstLastName,
        string? middleName = null,
        string? secondLastName = null
    )
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name cannot be null or empty.", nameof(firstName));

        if (firstName.Trim().Length < 2)
            throw new ArgumentException(
                "First name must be at least 2 characters long.",
                nameof(firstName)
            );

        if (string.IsNullOrWhiteSpace(firstLastName))
            throw new ArgumentException(
                "First last name cannot be null or empty.",
                nameof(firstLastName)
            );

        if (firstLastName.Trim().Length < 2)
            throw new ArgumentException(
                "First last name must be at least 2 characters long.",
                nameof(firstLastName)
            );

        if (middleName is not null && middleName.Trim().Length < 2)
            throw new ArgumentException(
                "Middle name must be at least 2 characters long if provided.",
                nameof(middleName)
            );

        if (secondLastName is not null && secondLastName.Trim().Length < 2)
            throw new ArgumentException(
                "Second last name must be at least 2 characters long if provided.",
                nameof(secondLastName)
            );

        return new PersonName(
            Capitalize(firstName.Trim()),
            string.IsNullOrWhiteSpace(middleName) ? null : Capitalize(middleName.Trim()),
            Capitalize(firstLastName.Trim()),
            string.IsNullOrWhiteSpace(secondLastName) ? null : Capitalize(secondLastName.Trim())
        );
    }

    public string FullName
    {
        get
        {
            var parts = new List<string> { FirstName };
            if (MiddleName is not null)
                parts.Add(MiddleName);
            parts.Add(FirstLastName);
            if (SecondLastName is not null)
                parts.Add(SecondLastName);
            return string.Join(' ', parts);
        }
    }

    public string DisplayName => $"{FirstName} {FirstLastName}";

    public string LastNames =>
        SecondLastName is not null ? $"{FirstLastName} {SecondLastName}" : FirstLastName;

    private static string Capitalize(string value) =>
        string.Join(
            ' ',
            value
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(word =>
                    string.Join(
                        "-",
                        word.Split('-')
                            .Select(part =>
                                part.Length == 0
                                    ? part
                                    : char.ToUpperInvariant(part[0]) + part[1..].ToLowerInvariant()
                            )
                    )
                )
        );

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return FirstName;
        yield return MiddleName;
        yield return FirstLastName;
        yield return SecondLastName;
    }

    public override string ToString() => FullName;
}
