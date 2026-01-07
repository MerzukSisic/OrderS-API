namespace OrdersAPI.Domain.Entities;

public class Accompaniment
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty; // "Pomfrit", "Riza", "Kupus", "Krastavice"
    public decimal ExtraCharge { get; set; } = 0; // Dodatna cijena (0 ako nema)
    public Guid AccompanimentGroupId { get; set; }
    public int DisplayOrder { get; set; } // Redoslijed prikazivanja
    public bool IsAvailable { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public AccompanimentGroup AccompanimentGroup { get; set; } = null!;
    public ICollection<OrderItemAccompaniment> OrderItemAccompaniments { get; set; } = new List<OrderItemAccompaniment>();
}