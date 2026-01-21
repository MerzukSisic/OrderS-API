namespace OrdersAPI.Application.DTOs;

public class CreatePaymentIntentDto
{
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "bam"; // BAM/KM for Bosnia
    public string? CustomerEmail { get; set; }
    public string? CustomerName { get; set; }
    public string? TableNumber { get; set; }
}

public class PaymentIntentResponseDto
{
    public string PaymentIntentId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class RefundRequestDto
{
    public string PaymentIntentId { get; set; } = string.Empty;
    public decimal? Amount { get; set; } // null = full refund
    public string Reason { get; set; } = "requested_by_customer";
}

public class RefundResponseDto
{
    public string RefundId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public class WebhookEventDto
{
    public string EventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string PaymentIntentId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}