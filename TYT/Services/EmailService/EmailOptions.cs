// Fà bindding con i dati in appsetting relativi alle email grazie a builder.Services.Configure<EmailOptions>

namespace TYT.Services.EmailService;

public sealed class EmailOptions
{
    public bool Enable { get; set; } = true;
    public string TemplatesPath { get; set; } = "Templates";
    public SmtpOptions Smtp { get; set; } = new();

    public sealed class SmtpOptions
    {
        public string Host { get; set; } = "";
        public int Port { get; set; } = 587;
        public string FromAddress { get; set; } = "";
        public string? FromDisplayName { get; set; }
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public bool EnableSsl { get; set; } = true;
        public bool UseDefaultCredentials { get; set; } = false;
    }
}
