namespace Identity.Domain.ValueObjects;

public sealed class RedirectUri : ValueObject
{
    public string Value { get; }

    private RedirectUri(string value) => Value = value;

    public static RedirectUri Create(string? value, bool isDevelopment = false)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("redirect_uri cannot be null or empty.", nameof(value));

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
            throw new ArgumentException($"'{value}' is not a valid URI.", nameof(value));

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            throw new ArgumentException($"'{value}' must use http or https scheme.", nameof(value));

        var isLocalhost =
            uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
            || uri.Host.Equals("[::1]", StringComparison.OrdinalIgnoreCase);

        if (!isDevelopment && uri.Scheme == Uri.UriSchemeHttp && !isLocalhost)
            throw new ArgumentException(
                $"'{value}' is not allowed in production. Only https URIs or http URIs pointing to localhost are allowed.",
                nameof(value)
            );

        if (!string.IsNullOrEmpty(uri.Fragment))
            throw new ArgumentException($"'{value}' cannot contain a fragment.", nameof(value));

        return new RedirectUri(uri.ToString().Trim());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
