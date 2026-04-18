namespace Identity.Domain.ValueObjects;

/// <summary>
/// URI de redirección válida para una app cliente OAuth.
///
/// Reglas de validación:
///   - Debe ser una URI bien formada
///   - Solo esquemas HTTP y HTTPS
///   - En producción: HTTPS obligatorio (excepto localhost)
///   - No puede tener fragmentos (#) — OAuth lo prohíbe
///   - Sin wildcards — comparación exacta siempre
///
/// URIs válidas en desarrollo:
///   http://localhost:3002/api/auth/callback  ✅
///
/// URIs válidas en producción:
///   https://portal.domain.com/api/auth/callback  ✅
///
/// URIs inválidas:
///   http://portal.domain.com/callback  ❌ (HTTP en producción)
///   https://app.com/callback#fragment  ❌ (fragmento)
///   ftp://app.com/callback             ❌ (esquema inválido)
/// </summary>
public sealed class RedirectUri : ValueObject
{
    public string Value { get; }

    private RedirectUri(string value) => Value = value;

    /// <param name="isDevelopment">
    /// Si true, permite HTTP en cualquier host.
    /// Si false, solo permite HTTP en localhost.
    /// </param>
    public static RedirectUri Create(string? value, bool isDevelopment = false)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("La redirect_uri no puede estar vacía.", nameof(value));

        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri))
            throw new ArgumentException($"'{value}' no es una URI válida.", nameof(value));

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            throw new ArgumentException(
                $"La redirect_uri debe usar HTTP o HTTPS. Esquema: '{uri.Scheme}'",
                nameof(value)
            );

        var isLocalhost = uri.Host == "localhost" || uri.Host == "127.0.0.1";

        if (!isDevelopment && uri.Scheme == Uri.UriSchemeHttp && !isLocalhost)
            throw new ArgumentException(
                "En producción la redirect_uri debe usar HTTPS. "
                    + "HTTP solo está permitido en localhost.",
                nameof(value)
            );

        // OAuth prohíbe fragmentos en redirect_uri
        // Los fragmentos (#) no se envían al servidor — son solo para el browser
        if (!string.IsNullOrEmpty(uri.Fragment))
            throw new ArgumentException(
                "La redirect_uri no puede contener fragmentos (#).",
                nameof(value)
            );

        return new RedirectUri(value.Trim());
    }

    /// <summary>
    /// Comparación exacta con la URI recibida en un request OAuth.
    /// OAuth exige comparación exacta — sin normalización de
    /// trailing slashes ni case-insensitive.
    ///
    /// "http://localhost:3002/api/auth/callback"  → match ✅
    /// "http://localhost:3002/api/auth/callback/" → NO match ❌ (trailing slash)
    /// </summary>
    public bool Matches(string requestedUri) =>
        string.Equals(Value, requestedUri, StringComparison.Ordinal);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
