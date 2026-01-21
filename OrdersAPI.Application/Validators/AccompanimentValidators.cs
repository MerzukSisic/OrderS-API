using FluentValidation;
using OrdersAPI.Application.DTOs;

namespace OrdersAPI.Application.Validators;

public class CreateAccompanimentGroupDtoValidator : AbstractValidator<CreateAccompanimentGroupDto>
{
    public CreateAccompanimentGroupDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Ime grupe je obavezno")
            .MaximumLength(100).WithMessage("Ime ne može biti duže od 100 karaktera");

        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("ProductId je obavezan");

        RuleFor(x => x.SelectionType)
            .NotEmpty().WithMessage("Selection type je obavezan")
            .Must(x => x == "Single" || x == "Multiple")
            .WithMessage("Selection type mora biti: Single ili Multiple");

        RuleFor(x => x.MinSelections)
            .GreaterThanOrEqualTo(0).WithMessage("MinSelections ne može biti negativan")
            .When(x => x.MinSelections.HasValue);

        RuleFor(x => x.MaxSelections)
            .GreaterThan(0).WithMessage("MaxSelections mora biti veći od 0")
            .GreaterThanOrEqualTo(x => x.MinSelections ?? 0)
            .WithMessage("MaxSelections mora biti veći ili jednak MinSelections")
            .When(x => x.MaxSelections.HasValue);
    }
}

public class CreateAccompanimentDtoValidator : AbstractValidator<CreateAccompanimentDto>
{
    public CreateAccompanimentDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Ime priloga je obavezno")
            .MaximumLength(100).WithMessage("Ime ne može biti duže od 100 karaktera");

        RuleFor(x => x.ExtraCharge)
            .GreaterThanOrEqualTo(0).WithMessage("ExtraCharge ne može biti negativan");

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0).WithMessage("DisplayOrder ne može biti negativan");
    }
}

public class UpdateAccompanimentGroupDtoValidator : AbstractValidator<UpdateAccompanimentGroupDto>
{
    public UpdateAccompanimentGroupDtoValidator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(100).WithMessage("Ime ne može biti duže od 100 karaktera")
            .When(x => !string.IsNullOrEmpty(x.Name));

        RuleFor(x => x.SelectionType)
            .Must(x => x == "Single" || x == "Multiple")
            .WithMessage("Selection type mora biti: Single ili Multiple")
            .When(x => !string.IsNullOrEmpty(x.SelectionType));
    }
}

public class UpdateAccompanimentDtoValidator : AbstractValidator<UpdateAccompanimentDto>
{
    public UpdateAccompanimentDtoValidator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(100).WithMessage("Ime ne može biti duže od 100 karaktera")
            .When(x => !string.IsNullOrEmpty(x.Name));

        RuleFor(x => x.ExtraCharge)
            .GreaterThanOrEqualTo(0).WithMessage("ExtraCharge ne može biti negativan");
    }
}
