using Identity.Application.Common.Models;
using MediatR;

namespace Identity.Application.OAuth.Commands.Authorize;

public record AuthorizeCommand(
    string ClientId,
    string RedirectUri,
    string ResponseType,
    string CodeChallenge,
    string CodeChallengeMethod,
    string? State,
    string? Scope,
    string? SessionId // ← nuevo: cookie idp_session del request
) : IRequest<Result<AuthorizeResult>>;

public record AuthorizeResult(
    string Url,
    bool RequiresLogin // false = SSO → redirect directo con code
);
