using System;
using System.Collections.Generic;

namespace OrdersAPI.Infrastructure.Data.Generated;

public partial class VwDailyRevenue
{
    public DateOnly? OrderDate { get; set; }

    public int? TotalOrders { get; set; }

    public decimal? TotalRevenue { get; set; }

    public decimal? AverageOrderValue { get; set; }
}
