using FluentValidation;
using OrdersAPI.Application.DTOs;

namespace OrdersAPI.Application.Validators;

public class CreateUserDtoValidator : AbstractValidator<CreateUserDto>
{
    public CreateUserDtoValidator()
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
            .MinimumLength(6).WithMessage("Password mora imati najmanje 6 karaktera");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Rola je obavezna")
            .Must(x => x == "Admin" || x == "Waiter" || x == "Bartender")
            .WithMessage("Rola mora biti: Admin, Waiter ili Bartender");
    }
}

public class UpdateUserDtoValidator : AbstractValidator<UpdateUserDto>
{
    public UpdateUserDtoValidator()
    {
        RuleFor(x => x.FullName)
            .MinimumLength(2).WithMessage("Ime mora imati najmanje 2 karaktera")
            .MaximumLength(100).WithMessage("Ime ne može biti duže od 100 karaktera")
            .When(x => x.FullName != null);
    }
}
