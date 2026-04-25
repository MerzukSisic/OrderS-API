using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OrdersAPI.Application.Interfaces;

namespace OrdersAPI.Infrastructure.Services;

/// <summary>
/// Dev-mode email sender: logs the email body instead of sending it over SMTP.
/// Used when Email:Driver is not "smtp" (or is absent).
/// </summary>
public class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string to, string subject, string body)
    {
        logger.LogInformation(
            "[EMAIL-DEV] To: {To} | Subject: {Subject}\n{Body}",
            to, subject, body);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Production SMTP email sender. Requires Email:Smtp:* configuration.
/// </summary>
public class SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(string to, string subject, string body)
    {
        var host = configuration["Email:Smtp:Host"]
            ?? throw new InvalidOperationException("Email:Smtp:Host not configured");
        var port = int.Parse(configuration["Email:Smtp:Port"] ?? "587");
        var user = configuration["Email:Smtp:User"] ?? string.Empty;
        var password = configuration["Email:Smtp:Password"] ?? string.Empty;
        var from = configuration["Email:Smtp:From"] ?? user;

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = true,
            Credentials = string.IsNullOrEmpty(user)
                ? null
                : new NetworkCredential(user, password)
        };

        var message = new MailMessage(from, to, subject, body) { IsBodyHtml = true };

        await client.SendMailAsync(message);
        logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
    }
}
