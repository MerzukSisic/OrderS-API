namespace OrdersAPI.Application.Interfaces;

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body);
}
