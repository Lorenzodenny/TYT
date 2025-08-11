// carica file HTML dalla cartella Templates, sostituisce i placeholder

using Microsoft.Extensions.Options;
using static TYT.Services.EmailService.EmailTemplateService;

namespace TYT.Services.EmailService;

public interface IEmailTemplateService
{
    EmailContent Generate(TemplateType type, Dictionary<FieldType, string> values);
    string LoadRaw(TemplateType type, string extension = "html");
}

public sealed class EmailTemplateService : IEmailTemplateService
{
    private readonly EmailOptions _opt;
    private readonly string _templateRoot;

    public EmailTemplateService(IOptions<EmailOptions> options, IWebHostEnvironment env)
    {
        _opt = options.Value;
        _templateRoot = Path.IsPathRooted(_opt.TemplatesPath)
            ? _opt.TemplatesPath
            : Path.Combine(env.ContentRootPath, _opt.TemplatesPath);
    }

    // solo i 3 template necessari
    public enum TemplateType
    {
        EmailConfirm,
        SendOTP,
        ResetPassword
    }

    // solo i placeholder necessari
    public enum FieldType
    {
        Nome,
        ConfirmationLink,
        CodiceOTP,
        ResetPasswordLink
    }

    public static readonly Dictionary<TemplateType, string> TemplateFiles = new()
    {
        { TemplateType.EmailConfirm, "EmailConfirmation" },
        { TemplateType.SendOTP, "OtpEmail" },
        { TemplateType.ResetPassword, "PasswordReset" }
    };

    public static readonly Dictionary<TemplateType, string> Subjects = new()
    {
        { TemplateType.EmailConfirm, "Conferma indirizzo email" },
        { TemplateType.SendOTP, "Codice di conferma" },
        { TemplateType.ResetPassword, "Reset della password" }
    };

    public EmailContent Generate(TemplateType type, Dictionary<FieldType, string> values)
    {
        var html = LoadRaw(type, "html");
        foreach (var (key, val) in values)
        {
            var p1 = "{{" + key + "}}";
            var p2 = "{" + key + "}";
            html = html.Replace(p1, val ?? string.Empty)
                       .Replace(p2, val ?? string.Empty);
        }

        var subject = Subjects[type];
        return new EmailContent(subject, html);
    }

    public string LoadRaw(TemplateType type, string extension = "html")
    {
        var fileName = TemplateFiles[type] + "." + extension;
        var path = Path.Combine(_templateRoot, fileName);
        if (!File.Exists(path)) return $"[MISSING TEMPLATE: {fileName}]";
        return File.ReadAllText(path);
    }

    public sealed record EmailContent(string Subject, string Html);
}

