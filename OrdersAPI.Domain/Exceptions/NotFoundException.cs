namespace OrdersAPI.Domain.Exceptions;

public class NotFoundException(string message) : Exception(message)
{
    public NotFoundException(string resourceName, object key)
        : this($"{resourceName} with id '{key}' was not found.") { }
}
