using System;
using System.Collections.Generic;

namespace OrdersAPI.Infrastructure.Data.Generated;

public partial class Notification
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Title { get; set; } = null!;

    public string Message { get; set; } = null!;

    public string Type { get; set; } = null!;

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
