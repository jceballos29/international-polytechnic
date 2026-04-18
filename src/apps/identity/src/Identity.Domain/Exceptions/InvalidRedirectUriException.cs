namespace Identity.Domain.Exceptions;

/// <summary>
/// La redirect_uri del request OAuth no está en la whitelist
/// de la app cliente. Error de seguridad crítico en OAuth.
/// No revelamos cuáles son las URIs permitidas.
/// </summary>
public class InvalidRedirectUriException : DomainException
{
    public InvalidRedirectUriException(string uri)
        : base(
            $"La redirect_uri '{uri}' no está autorizada para esta aplicación.",
            "INVALID_REDIRECT_URI",
            400
        ) { }
}
