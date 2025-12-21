using Microsoft.Extensions.Configuration;
using Stripe;

namespace OrdersAPI.Infrastructure.Services;

public class StripeService : IStripeService
{
    private readonly IConfiguration _configuration;

    public StripeService(IConfiguration configuration)
    {
        _configuration = configuration;
        StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
    }

    public async Task<string> CreatePaymentIntentAsync(decimal amount, string currency = "eur")
    {
        var options = new PaymentIntentCreateOptions
        {
            Amount = (long)(amount * 100), // Convert to cents
            Currency = currency,
            PaymentMethodTypes = new List<string> { "card" }
        };

        var service = new PaymentIntentService();
        var paymentIntent = await service.CreateAsync(options);
        
        return paymentIntent.ClientSecret;
    }

    public async Task<bool> ConfirmPaymentAsync(string paymentIntentId)
    {
        var service = new PaymentIntentService();
        var paymentIntent = await service.GetAsync(paymentIntentId);
        
        return paymentIntent.Status == "succeeded";
    }
}

