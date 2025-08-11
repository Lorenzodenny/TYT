// Invia-email: usa SmtpClient con i parametri delle EmailOptions e manda la mail HTML

using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;

namespace TYT.Services.EmailService;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _opt;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _opt = options.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string htmlMessage)
    {
        if (!_opt.Enable)
        {
            _logger.LogInformation("[EMAIL DISABLED] To={To} Subject={Subject}", to, subject);
            return;
        }

        using var client = new SmtpClient(_opt.Smtp.Host, _opt.Smtp.Port)
        {
            EnableSsl = _opt.Smtp.EnableSsl,
            UseDefaultCredentials = _opt.Smtp.UseDefaultCredentials,
            Credentials = _opt.Smtp.UseDefaultCredentials
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(_opt.Smtp.Username, _opt.Smtp.Password)
        };

        var from = new MailAddress(_opt.Smtp.FromAddress, _opt.Smtp.FromDisplayName);
        using var msg = new MailMessage
        {
            From = from,
            Subject = subject,
            Body = htmlMessage,
            IsBodyHtml = true
        };
        msg.To.Add(new MailAddress(to));

        await client.SendMailAsync(msg);
        _logger.LogInformation("Email inviata: To={To} Subject={Subject}", to, subject);
    }
}
