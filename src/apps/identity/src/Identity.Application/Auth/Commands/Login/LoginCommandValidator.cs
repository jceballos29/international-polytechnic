using FluentValidation;

namespace Identity.Application.Auth.Commands.Login;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("El email es requerido.")
            .EmailAddress()
            .WithMessage("El email no tiene un formato válido.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("La contraseña es requerida.")
            .MinimumLength(6)
            .WithMessage("Mínimo 6 caracteres.");

        // Validación condicional — solo si viene en modo OAuth
        When(
            x => x.ClientId is not null,
            () =>
            {
                RuleFor(x => x.RedirectUri).NotEmpty().WithMessage("La redirect_uri es requerida.");

                RuleFor(x => x.CodeChallenge)
                    .NotEmpty()
                    .WithMessage("El code_challenge es requerido.");

                RuleFor(x => x.CodeChallengeMethod)
                    .NotEmpty()
                    .Must(m => m == "S256")
                    .WithMessage("Solo se soporta code_challenge_method=S256.");
            }
        );
    }
}
