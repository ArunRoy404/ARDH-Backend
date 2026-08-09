using CleanArchitecture.Application.Common;
using CleanArchitecture.Application.Common.Interfaces;
using Resend;

namespace CleanArchitecture.Application.Services;

public class MailService(AppSettings appSettings, IResend resend) : IMailService
{
    private readonly MailConfigurations _mailSettings = appSettings.MailConfigurations;
    private readonly IResend _resend = resend;

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        string fromMail = _mailSettings.From;
        string fromDisplayName = string.IsNullOrWhiteSpace(_mailSettings.DisplayName) ? fromMail : _mailSettings.DisplayName;
        string from = fromDisplayName == fromMail ? fromMail : $"{fromDisplayName} <{fromMail}>";

        var message = new EmailMessage
        {
            From = from,
            Subject = subject,
            HtmlBody = "<html><body>" + htmlMessage + "</body></html>"
        };
        message.To.Add(email);

        await _resend.EmailSendAsync(message);
    }
}
