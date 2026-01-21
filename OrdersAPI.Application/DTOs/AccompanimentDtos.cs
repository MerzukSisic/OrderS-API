namespace OrdersAPI.Application.DTOs;

public class CreateAccompanimentGroupDto
{
    public string Name { get; set; } = string.Empty;
    public Guid ProductId { get; set; }
    public string SelectionType { get; set; } = "Single";
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
