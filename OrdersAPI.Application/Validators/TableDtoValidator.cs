using FluentValidation;
using OrdersAPI.Application.DTOs;

namespace OrdersAPI.Application.Validators;

public class CreateTableDtoValidator : AbstractValidator<CreateTableDto>
{
    public CreateTableDtoValidator()
    {
        RuleFor(x => x.TableNumber)
            .NotEmpty().WithMessage("Broj stola je obavezan")
            .MaximumLength(10).WithMessage("Broj stola ne može biti duži od 10 karaktera");

        RuleFor(x => x.Capacity)
            .GreaterThan(0).WithMessage("Kapacitet mora biti veci od 0")
            .LessThanOrEqualTo(20).WithMessage("Kapacitet ne može biti veci od 20");
    }
}

public class UpdateTableDtoValidator : AbstractValidator<UpdateTableDto>
{
    public UpdateTableDtoValidator()
    {
        RuleFor(x => x.TableNumber)
            .MaximumLength(10).WithMessage("Broj stola ne može biti duži od 10 karaktera")
            .When(x => x.TableNumber != null);

        RuleFor(x => x.Capacity)
            .GreaterThan(0).WithMessage("Kapacitet mora biti veci od 0")
            .LessThanOrEqualTo(20).WithMessage("Kapacitet ne može biti veci od 20")
            .When(x => x.Capacity.HasValue);

        RuleFor(x => x.Status)
            .Must(x => x == "Available" || x == "Occupied" || x == "Reserved")
            .WithMessage("Status mora biti: Available, Occupied ili Reserved")
            .When(x => x.Status != null);
    }
}
