using FluentValidation;
using OrdersAPI.Application.DTOs;

namespace OrdersAPI.Application.Validators;

public class ChangePasswordDtoValidator : AbstractValidator<ChangePasswordDto>
{
    public ChangePasswordDtoValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Trenutni password je obavezan");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Novi password je obavezan")
            .MinimumLength(6).WithMessage("Password mora imati najmanje 6 karaktera")
            .NotEqual(x => x.CurrentPassword).WithMessage("Novi password mora biti različit od trenutnog");
    }
}

public class ResetPasswordDtoValidator : AbstractValidator<ResetPasswordDto>
{
    public ResetPasswordDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email je obavezan")
            .EmailAddress().WithMessage("Email mora biti validna email adresa");

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token je obavezan");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Novi password je obavezan")
            .MinimumLength(6).WithMessage("Password mora imati najmanje 6 karaktera");
    }
}