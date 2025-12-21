using FluentValidation;
using OrdersAPI.Application.DTOs;

namespace OrdersAPI.Application.Validators;

public class CreateProductDtoValidator : AbstractValidator<CreateProductDto>
{
    public CreateProductDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Ime proizvoda je obavezno")
            .MinimumLength(2).WithMessage("Ime mora imati najmanje 2 karaktera")
            .MaximumLength(100).WithMessage("Ime ne može biti duže od 100 karaktera");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Cijena mora biti veca od 0")
            .LessThanOrEqualTo(10000).WithMessage("Cijena ne može biti veca od 10000");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Kategorija je obavezna");

        RuleFor(x => x.PreparationLocation)
            .NotEmpty().WithMessage("Preparation location je obavezan")
            .Must(x => x == "Kitchen" || x == "Bar")
            .WithMessage("Preparation location mora biti: Kitchen ili Bar");

        RuleFor(x => x.PreparationTimeMinutes)
            .GreaterThan(0).WithMessage("Vrijeme pripreme mora biti vece od 0")
            .LessThanOrEqualTo(180).WithMessage("Vrijeme pripreme ne može biti vece od 180 minuta");

        RuleFor(x => x.Stock)
            .GreaterThanOrEqualTo(0).WithMessage("Stock ne može biti negativan");
    }
}

public class UpdateProductDtoValidator : AbstractValidator<UpdateProductDto>
{
    public UpdateProductDtoValidator()
    {
        RuleFor(x => x.Name)
            .MinimumLength(2).WithMessage("Ime mora imati najmanje 2 karaktera")
            .MaximumLength(100).WithMessage("Ime ne može biti duže od 100 karaktera")
            .When(x => x.Name != null);

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Cijena mora biti veca od 0")
            .LessThanOrEqualTo(10000).WithMessage("Cijena ne može biti veca od 10000")
            .When(x => x.Price.HasValue);

        RuleFor(x => x.PreparationLocation)
            .Must(x => x == "Kitchen" || x == "Bar")
            .WithMessage("Preparation location mora biti: Kitchen ili Bar")
            .When(x => x.PreparationLocation != null);

        RuleFor(x => x.PreparationTimeMinutes)
            .GreaterThan(0).WithMessage("Vrijeme pripreme mora biti vece od 0")
            .LessThanOrEqualTo(180).WithMessage("Vrijeme pripreme ne može biti vece od 180 minuta")
            .When(x => x.PreparationTimeMinutes.HasValue);

        RuleFor(x => x.Stock)
            .GreaterThanOrEqualTo(0).WithMessage("Stock ne može biti negativan")
            .When(x => x.Stock.HasValue);
    }
}
