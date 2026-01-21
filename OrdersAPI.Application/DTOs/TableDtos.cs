namespace OrdersAPI.Application.DTOs;

public class TableDto
{
    public Guid Id { get; set; }
    public string TableNumber { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Location { get; set; }
    public Guid? CurrentOrderId { get; set; }
    public decimal? CurrentOrderTotal { get; set; }
    public int ActiveOrderCount { get; set; }
}

public class CreateTableDto
{
    public string TableNumber { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string? Location { get; set; }
}

public class UpdateTableDto
{
    public string? TableNumber { get; set; }
    public int? Capacity { get; set; }
    public string? Status { get; set; }
    public string? Location { get; set; }
}