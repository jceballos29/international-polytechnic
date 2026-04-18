using FluentValidation;

namespace Identity.Application.OAuth.Commands.ExchangeCode;

public class ExchangeCodeCommandValidator : AbstractValidator<ExchangeCodeCommand>
{
    public ExchangeCodeCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage("El code es requerido.");

        RuleFor(x => x.CodeVerifier)
            .NotEmpty()
            .WithMessage("El code_verifier es requerido.")
            .MinimumLength(43)
            .WithMessage("El code_verifier debe tener al menos 43 caracteres.")
            .MaximumLength(128)
            .WithMessage("El code_verifier no puede tener más de 128 caracteres.");

        RuleFor(x => x.ClientId).NotEmpty().WithMessage("El client_id es requerido.");

        RuleFor(x => x.RedirectUri).NotEmpty().WithMessage("La redirect_uri es requerida.");
    }
}
