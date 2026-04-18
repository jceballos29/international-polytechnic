using FluentValidation;

namespace Identity.Application.OAuth.Commands.Authorize;

public class AuthorizeCommandValidator : AbstractValidator<AuthorizeCommand>
{
    public AuthorizeCommandValidator()
    {
        RuleFor(x => x.ClientId).NotEmpty().WithMessage("client_id es requerido.");

        RuleFor(x => x.RedirectUri).NotEmpty().WithMessage("redirect_uri es requerida.");

        RuleFor(x => x.ResponseType)
            .NotEmpty()
            .Must(rt => rt == "code")
            .WithMessage("Solo se soporta response_type=code.");

        RuleFor(x => x.CodeChallenge)
            .NotEmpty()
            .WithMessage("code_challenge es requerido (PKCE obligatorio).");

        RuleFor(x => x.CodeChallengeMethod)
            .NotEmpty()
            .Must(m => m == "S256")
            .WithMessage("Solo se soporta code_challenge_method=S256.");
    }
}
