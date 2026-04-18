using FluentValidation;

namespace Identity.Application.OAuth.Commands.RefreshToken;

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().WithMessage("El refresh_token es requerido.");

        RuleFor(x => x.ClientId).NotEmpty().WithMessage("El client_id es requerido.");
    }
}
