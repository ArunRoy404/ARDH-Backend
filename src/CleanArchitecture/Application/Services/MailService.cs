using CleanArchitecture.Application.Common;
using CleanArchitecture.Application.Common.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace CleanArchitecture.Application.Services;

public class MailService(AppSettings appSettings) : IMailService
{
    private readonly MailConfigurations _mailSettings = appSettings.MailConfigurations;

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var fromDisplayName = string.IsNullOrWhiteSpace(_mailSettings.DisplayName) ? _mailSettings.From : _mailSettings.DisplayName;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromDisplayName, _mailSettings.From));
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = "<html><body>" + htmlMessage + "</body></html>" };

        using var client = new SmtpClient();
        var secureSocketOptions = _mailSettings.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
        await client.ConnectAsync(_mailSettings.Host, _mailSettings.Port, secureSocketOptions);
        await client.AuthenticateAsync(_mailSettings.Username, _mailSettings.Password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
