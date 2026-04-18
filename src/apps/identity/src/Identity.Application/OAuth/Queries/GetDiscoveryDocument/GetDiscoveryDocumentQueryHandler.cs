using MediatR;

namespace Identity.Application.OAuth.Queries.GetDiscoveryDocument;

public class GetDiscoveryDocumentQueryHandler
    : IRequestHandler<GetDiscoveryDocumentQuery, DiscoveryDocumentResult>
{
    public Task<DiscoveryDocumentResult> Handle(
        GetDiscoveryDocumentQuery request,
        CancellationToken cancellationToken
    )
    {
        var base_ = request.BaseUrl.TrimEnd('/');

        var result = new DiscoveryDocumentResult(
            Issuer: base_,
            AuthorizationEndpoint: $"{base_}/oauth/authorize",
            TokenEndpoint: $"{base_}/oauth/token",
            UserInfoEndpoint: $"{base_}/oauth/userinfo",
            JwksUri: $"{base_}/.well-known/jwks.json",
            RevocationEndpoint: $"{base_}/oauth/revoke",
            ResponseTypesSupported: ["code"],
            GrantTypesSupported: ["authorization_code", "client_credentials", "refresh_token"],
            SubjectTypesSupported: ["public"],
            IdTokenSigningAlgValuesSupported: ["RS256"],
            ScopesSupported: ["openid", "profile", "email"]
        );

        return Task.FromResult(result);
    }
}
