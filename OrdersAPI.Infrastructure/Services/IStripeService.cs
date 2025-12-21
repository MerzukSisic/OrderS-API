namespace OrdersAPI.Infrastructure.Services;

public interface IStripeService
{
    Task<string> CreatePaymentIntentAsync(decimal amount, string currency = "eur");
    Task<bool> ConfirmPaymentAsync(string paymentIntentId);
}
