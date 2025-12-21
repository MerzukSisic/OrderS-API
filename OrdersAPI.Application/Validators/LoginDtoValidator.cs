using FluentValidation;
using OrdersAPI.Application.DTOs;

namespace OrdersAPI.Application.Validators;

public class LoginDtoValidator : AbstractValidator<LoginDto>
{
    public LoginDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email je obavezan")
            .EmailAddress().WithMessage("Email mora biti validna email adresa");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password je obavezan")
            .MinimumLength(6).WithMessage("Password mora imati najmanje 6 karaktera");
    }
}
