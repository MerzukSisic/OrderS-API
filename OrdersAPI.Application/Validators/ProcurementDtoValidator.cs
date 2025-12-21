using FluentValidation;
using OrdersAPI.Application.DTOs;

namespace OrdersAPI.Application.Validators;

public class CreateProcurementDtoValidator : AbstractValidator<CreateProcurementDto>
{
    public CreateProcurementDtoValidator()
    {
        RuleFor(x => x.StoreId)
            .NotEmpty().WithMessage("StoreId je obavezan");

        RuleFor(x => x.Supplier)
            .NotEmpty().WithMessage("Dobavljac je obavezan")
            .MinimumLength(2).WithMessage("Dobavljac mora imati najmanje 2 karaktera")
            .MaximumLength(100).WithMessage("Dobavljac ne može biti duži od 100 karaktera");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Narudžba mora imati najmanje jednu stavku")
            .Must(x => x.Count > 0).WithMessage("Narudžba mora imati najmanje jednu stavku");

        RuleForEach(x => x.Items).SetValidator(new CreateProcurementItemDtoValidator());
    }
}

public class CreateProcurementItemDtoValidator : AbstractValidator<CreateProcurementItemDto>
{
    public CreateProcurementItemDtoValidator()
    {
        RuleFor(x => x.StoreProductId)
            .NotEmpty().WithMessage("StoreProductId je obavezan");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Kolicina mora biti veca od 0")
            .LessThanOrEqualTo(10000).WithMessage("Kolicina ne može biti veca od 10000");
    }
}
