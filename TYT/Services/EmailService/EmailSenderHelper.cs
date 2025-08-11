// prepara i contenuti e li invia

using Microsoft.AspNetCore.Identity.UI.Services;
using TYT.Models;
using static TYT.Services.EmailService.EmailTemplateService;

namespace TYT.Services.EmailService;

public sealed class EmailSenderHelper
{
    private readonly IHttpContextAccessor _http;
    private readonly IEmailSender _sender;
    private readonly IEmailTemplateService _tpl;

    public EmailSenderHelper(IHttpContextAccessor http, IEmailSender sender, IEmailTemplateService tpl)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        _tpl = tpl ?? throw new ArgumentNullException(nameof(tpl));
    }

    public async Task SendEmailConfirmationAsync(TYTUser user, string confirmationToken)
    {
        var req = _http.HttpContext?.Request;
        if (req is null) throw new InvalidOperationException("HttpContext non disponibile.");

        var link = $"{req.Scheme}://{req.Host}/api/auth/confirm-email?userId={Uri.EscapeDataString(user.Id)}&token={Uri.EscapeDataString(confirmationToken)}";

        var content = _tpl.Generate(TemplateType.EmailConfirm, new()
        {
            { FieldType.Nome, user.Nome ?? user.Email ?? "Utente" },
            { FieldType.ConfirmationLink, link }
        });

        await _sender.SendEmailAsync(user.Email!, content.Subject, content.Html);
    }

    public async Task SendOtpAsync(TYTUser user, string otp)
    {
        var content = _tpl.Generate(TemplateType.SendOTP, new()
        {
            { FieldType.Nome, user.Nome ?? user.Email ?? "Utente" },
            { FieldType.CodiceOTP, otp }
        });

        await _sender.SendEmailAsync(user.Email!, content.Subject, content.Html);
    }

    public async Task SendPasswordResetAsync(TYTUser user, string resetToken, string frontendResetUrlBase)
    {
        // Esempio: frontendResetUrlBase = "https://app.tyt.com/reset-password"
        var resetLink = $"{frontendResetUrlBase}?token={Uri.EscapeDataString(resetToken)}&email={Uri.EscapeDataString(user.Email!)}";

        var content = _tpl.Generate(TemplateType.ResetPassword, new()
        {
            { FieldType.Nome, user.Nome ?? user.Email ?? "Utente" },
            { FieldType.ResetPasswordLink, resetLink }
        });

        await _sender.SendEmailAsync(user.Email!, content.Subject, content.Html);
    }
}
