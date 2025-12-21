using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using OrdersAPI.Application.Validators;

namespace OrdersAPI.Application.Extensions;

public static class ValidationExtensions
{
    public static IServiceCollection AddValidators(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<LoginDtoValidator>();
        return services;
    }
}
