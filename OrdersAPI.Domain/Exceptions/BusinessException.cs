namespace OrdersAPI.Domain.Exceptions;

public class BusinessException(string message) : Exception(message);
