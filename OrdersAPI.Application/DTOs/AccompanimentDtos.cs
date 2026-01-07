namespace OrdersAPI.Application.DTOs;

// ============= Request DTOs =============

public class CreateAccompanimentGroupDto
{
    public string Name { get; set; } = string.Empty; // "Garnitura", "Dodatci", "Povrće"
    public Guid ProductId { get; set; }
    public string SelectionType { get; set; } = "Single"; // "Single" ili "Multiple"
    public bool IsRequired { get; set; }
    public int? MinSelections { get; set; }
    public int? MaxSelections { get; set; }
    public int DisplayOrder { get; set; }
    public List<CreateAccompanimentDto> Accompaniments { get; set; } = new();
}

public class CreateAccompanimentDto
{
    public string Name { get; set; } = string.Empty;
    public decimal ExtraCharge { get; set; } = 0;
    public int DisplayOrder { get; set; }
    public bool IsAvailable { get; set; } = true;
}

public class UpdateAccompanimentGroupDto
{
    public string Name { get; set; } = string.Empty;
    public string SelectionType { get; set; } = "Single";
    public bool IsRequired { get; set; }
    public int? MinSelections { get; set; }
    public int? MaxSelections { get; set; }
    public int DisplayOrder { get; set; }
}

public class UpdateAccompanimentDto
{
    public string Name { get; set; } = string.Empty;
    public decimal ExtraCharge { get; set; } = 0;
    public int DisplayOrder { get; set; }
    public bool IsAvailable { get; set; } = true;
}

// ============= Response DTOs =============

public class AccompanimentGroupDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid ProductId { get; set; }
    public string SelectionType { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public int? MinSelections { get; set; }
    public int? MaxSelections { get; set; }
    public int DisplayOrder { get; set; }
    public List<AccompanimentDto> Accompaniments { get; set; } = new();
}

public class AccompanimentDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal ExtraCharge { get; set; }
    public Guid AccompanimentGroupId { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsAvailable { get; set; }
}

// ============= Order-related DTOs =============

public class SelectedAccompanimentDto
{
    public Guid AccompanimentId { get; set; }
    public string Name { get; set; } = string.Empty; // Za prikaz
    public decimal ExtraCharge { get; set; } // Za računanje ukupne cijene
}

// Dodaj ovo u postojeći CreateOrderItemDto
public class CreateOrderItemWithAccompanimentsDto
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public string? Notes { get; set; }
    public List<Guid> SelectedAccompanimentIds { get; set; } = new(); // Lista izabranih priloga
}

// Update postojeći OrderItemDto da uključi priloge
public class OrderItemWithAccompanimentsDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<SelectedAccompanimentDto> SelectedAccompaniments { get; set; } = new();
}