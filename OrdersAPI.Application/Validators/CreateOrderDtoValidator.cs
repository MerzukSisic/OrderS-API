using FluentValidation;
using OrdersAPI.Application.DTOs;

namespace OrdersAPI.Application.Validators;

public class CreateOrderDtoValidator : AbstractValidator<CreateOrderDto>
{
    public CreateOrderDtoValidator()
    {
        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Tip narudžbe je obavezan")
            .Must(x => x == "DineIn" || x == "TakeAway")
            .WithMessage("Tip mora biti: DineIn ili TakeAway");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Narudžba mora imati najmanje jednu stavku")
            .Must(x => x.Count > 0).WithMessage("Narudžba mora imati najmanje jednu stavku");

        RuleForEach(x => x.Items).SetValidator(new CreateOrderItemDtoValidator());

        RuleFor(x => x.TableId)
            .NotEmpty()
            .When(x => x.Type == "DineIn")
            .WithMessage("TableId je obavezan za DineIn narudžbe");
    }
}

public class CreateOrderItemDtoValidator : AbstractValidator<CreateOrderItemDto>
{
    public CreateOrderItemDtoValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("ProductId je obavezan");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Kolicina mora biti veca od 0")
            .LessThanOrEqualTo(100).WithMessage("Kolicina ne može biti veca od 100");
    }
}
