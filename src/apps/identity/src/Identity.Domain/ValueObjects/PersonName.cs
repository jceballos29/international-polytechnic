namespace Identity.Domain.ValueObjects;

/// <summary>
/// Nombre completo de una persona.
///
/// Estructura latinoamericana:
///   FirstName      → Primer nombre   (obligatorio)
///   MiddleName     → Segundo nombre  (opcional)
///   FirstLastName  → Primer apellido (obligatorio)
///   SecondLastName → Segundo apellido (opcional)
///
/// Ejemplos:
///   PersonName.Create("Juan", "García")
///   PersonName.Create("Juan", "García", middleName: "Pablo")
///   PersonName.Create("María", "Rodríguez", secondLastName: "López")
///   PersonName.Create("Juan", "García", "Pablo", "López")
/// </summary>
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
            throw new ArgumentException("El primer nombre es requerido.", nameof(firstName));

        if (firstName.Trim().Length < 2)
            throw new ArgumentException(
                "El primer nombre debe tener al menos 2 caracteres.",
                nameof(firstName)
            );

        if (string.IsNullOrWhiteSpace(firstLastName))
            throw new ArgumentException("El primer apellido es requerido.", nameof(firstLastName));

        if (firstLastName.Trim().Length < 2)
            throw new ArgumentException(
                "El primer apellido debe tener al menos 2 caracteres.",
                nameof(firstLastName)
            );

        if (middleName is not null && middleName.Trim().Length < 2)
            throw new ArgumentException(
                "El segundo nombre debe tener al menos 2 caracteres.",
                nameof(middleName)
            );

        if (secondLastName is not null && secondLastName.Trim().Length < 2)
            throw new ArgumentException(
                "El segundo apellido debe tener al menos 2 caracteres.",
                nameof(secondLastName)
            );

        return new PersonName(
            Capitalize(firstName.Trim()),
            string.IsNullOrWhiteSpace(middleName) ? null : Capitalize(middleName.Trim()),
            Capitalize(firstLastName.Trim()),
            string.IsNullOrWhiteSpace(secondLastName) ? null : Capitalize(secondLastName.Trim())
        );
    }

    // ── Propiedades calculadas ─────────────────────────────

    /// <summary>
    /// Nombre completo con todos los campos presentes.
    /// "Juan Pablo García López"
    /// </summary>
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
            return string.Join(" ", parts);
        }
    }

    /// <summary>
    /// Primer nombre + primer apellido.
    /// Útil en interfaces donde el nombre completo es muy largo.
    /// "Juan García"
    /// </summary>
    public string DisplayName => $"{FirstName} {FirstLastName}";

    /// <summary>
    /// Iniciales del primer nombre y primer apellido.
    /// "Juan García" → "JG"
    /// Útil para avatares.
    /// </summary>
    public string Initials => $"{FirstName[0]}{FirstLastName[0]}".ToUpperInvariant();

    /// <summary>
    /// Apellidos completos.
    /// "García" o "García López"
    /// </summary>
    public string LastNames =>
        SecondLastName is null ? FirstLastName : $"{FirstLastName} {SecondLastName}";

    // ── Helpers privados ───────────────────────────────────

    /// <summary>
    /// Capitaliza la primera letra de cada palabra.
    /// Maneja nombres con guión: "María-José" → "María-José"
    /// </summary>
    private static string Capitalize(string value) =>
        string.Join(
            " ",
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
