namespace OrdersAPI.Domain.Constants;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Waiter = "Waiter";
    public const string Kitchen = "Kitchen";
    public const string Bartender = "Bartender";
    public const string Manager = "Manager";

    public const string AdminOrManager = $"{Admin},{Manager}";
    public const string AdminManagerOrWaiter = $"{Admin},{Manager},{Waiter}";
    public const string KitchenOrBar = $"{Admin},{Bartender},{Kitchen}";
    public const string AllStaff = $"{Waiter},{Bartender},{Kitchen},{Admin}";
}
