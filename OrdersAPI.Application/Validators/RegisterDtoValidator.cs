using FluentValidation;
using OrdersAPI.Application.DTOs;

namespace OrdersAPI.Application.Validators;

public class RegisterDtoValidator : AbstractValidator<RegisterDto>
{
    public RegisterDtoValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Puno ime je obavezno")
            .MinimumLength(2).WithMessage("Ime mora imati najmanje 2 karaktera")
            .MaximumLength(100).WithMessage("Ime ne može biti duže od 100 karaktera");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email je obavezan")
            .EmailAddress().WithMessage("Email mora biti validna email adresa");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password je obavezan")
            .MinimumLength(6).WithMessage("Password mora imati najmanje 6 karaktera")
            .Matches(@"[A-Z]").WithMessage("Password mora imati najmanje jedno veliko slovo")
            .Matches(@"[a-z]").WithMessage("Password mora imati najmanje jedno malo slovo")
            .Matches(@"[0-9]").WithMessage("Password mora imati najmanje jedan broj");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Rola je obavezna")
            .Must(x => x == "Admin" || x == "Waiter" || x == "Bartender")
            .WithMessage("Rola mora biti: Admin, Waiter ili Bartender");

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^\+?[0-9\s\-\(\)]+")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber))
            .WithMessage("Telefonski broj nije validan");
    }
}
