using OrdersAPI.Domain.Enums;

namespace OrdersAPI.Domain.Entities;

public class AccompanimentGroup
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty; // "Garnitura", "Povrće", "Dodatci"
    public Guid ProductId { get; set; }
    public SelectionType SelectionType { get; set; }
    public bool IsRequired { get; set; } // Da li MORA izabrati
    public int? MinSelections { get; set; } // Minimum broj izbora (npr. 1)
    public int? MaxSelections { get; set; } // Maximum broj izbora (npr. 3)
    public int DisplayOrder { get; set; } // Redoslijed prikazivanja
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Product Product { get; set; } = null!;
    public ICollection<Accompaniment> Accompaniments { get; set; } = new List<Accompaniment>();
}