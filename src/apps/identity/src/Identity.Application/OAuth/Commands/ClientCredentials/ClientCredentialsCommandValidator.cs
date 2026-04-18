using FluentValidation;

namespace Identity.Application.OAuth.Commands.ClientCredentials;

public class ClientCredentialsCommandValidator : AbstractValidator<ClientCredentialsCommand>
{
    public ClientCredentialsCommandValidator()
    {
        RuleFor(x => x.ClientId).NotEmpty().WithMessage("El client_id es requerido.");

        RuleFor(x => x.ClientSecret).NotEmpty().WithMessage("El client_secret es requerido.");

        RuleFor(x => x.Scope).NotEmpty().WithMessage("El scope es requerido.");
    }
}
