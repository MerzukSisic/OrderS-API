using FluentValidation;
using OrdersAPI.Application.DTOs;

namespace OrdersAPI.Application.Validators;

public class CreateStoreProductDtoValidator : AbstractValidator<CreateStoreProductDto>
{
    public CreateStoreProductDtoValidator()
    {
        RuleFor(x => x.StoreId)
            .NotEmpty().WithMessage("StoreId je obavezan");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Ime proizvoda je obavezno")
            .MinimumLength(2).WithMessage("Ime mora imati najmanje 2 karaktera")
            .MaximumLength(100).WithMessage("Ime ne može biti duže od 100 karaktera");

        RuleFor(x => x.PurchasePrice)
            .GreaterThan(0).WithMessage("Nabavna cijena mora biti veca od 0")
            .LessThanOrEqualTo(100000).WithMessage("Nabavna cijena ne može biti veca od 100000");

        RuleFor(x => x.CurrentStock)
            .GreaterThanOrEqualTo(0).WithMessage("Stock ne može biti negativan");

        RuleFor(x => x.MinimumStock)
            .GreaterThanOrEqualTo(0).WithMessage("Minimum stock ne može biti negativan");

        RuleFor(x => x.Unit)
            .NotEmpty().WithMessage("Jedinica mjere je obavezna")
            .MaximumLength(20).WithMessage("Jedinica mjere ne može biti duža od 20 karaktera");
    }
}

public class UpdateStoreProductDtoValidator : AbstractValidator<UpdateStoreProductDto>
{
    public UpdateStoreProductDtoValidator()
    {
        RuleFor(x => x.Name)
            .MinimumLength(2).WithMessage("Ime mora imati najmanje 2 karaktera")
            .MaximumLength(100).WithMessage("Ime ne može biti duže od 100 karaktera")
            .When(x => x.Name != null);

        RuleFor(x => x.PurchasePrice)
            .GreaterThan(0).WithMessage("Nabavna cijena mora biti veca od 0")
            .LessThanOrEqualTo(100000).WithMessage("Nabavna cijena ne može biti veca od 100000")
            .When(x => x.PurchasePrice.HasValue);

        RuleFor(x => x.CurrentStock)
            .GreaterThanOrEqualTo(0).WithMessage("Stock ne može biti negativan")
            .When(x => x.CurrentStock.HasValue);

        RuleFor(x => x.MinimumStock)
            .GreaterThanOrEqualTo(0).WithMessage("Minimum stock ne može biti negativan")
            .When(x => x.MinimumStock.HasValue);

        RuleFor(x => x.Unit)
            .MaximumLength(20).WithMessage("Jedinica mjere ne može biti duža od 20 karaktera")
            .When(x => x.Unit != null);
    }
}

public class AdjustInventoryDtoValidator : AbstractValidator<AdjustInventoryDto>
{
    public AdjustInventoryDtoValidator()
    {
        RuleFor(x => x.QuantityChange)
            .NotEqual(0).WithMessage("Promjena mora biti razlicita od 0")
            .GreaterThan(-10000).WithMessage("Promjena ne može biti manja od -10000")
            .LessThan(10000).WithMessage("Promjena ne može biti veca od 10000");

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Tip je obavezan")
            .Must(x => x == "Sale" || x == "Restock" || x == "Adjustment" || x == "Damage")
            .WithMessage("Tip mora biti: Sale, Restock, Adjustment ili Damage");
    }
}
