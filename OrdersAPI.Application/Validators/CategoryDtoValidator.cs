using FluentValidation;
using OrdersAPI.Application.DTOs;

namespace OrdersAPI.Application.Validators;

public class CreateCategoryDtoValidator : AbstractValidator<CreateCategoryDto>
{
    public CreateCategoryDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Ime kategorije je obavezno")
            .MinimumLength(2).WithMessage("Ime mora imati najmanje 2 karaktera")
            .MaximumLength(50).WithMessage("Ime ne mo�e biti du�e od 50 karaktera");
    }
}

public class UpdateCategoryDtoValidator : AbstractValidator<UpdateCategoryDto>
{
    public UpdateCategoryDtoValidator()
    {
        RuleFor(x => x.Name)
            .MinimumLength(2).WithMessage("Ime mora imati najmanje 2 karaktera")
            .MaximumLength(50).WithMessage("Ime ne mo�e biti du�e od 50 karaktera")
            .When(x => x.Name != null);
    }
}