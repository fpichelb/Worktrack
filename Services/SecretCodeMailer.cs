using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Worktrack.Models;

namespace Worktrack.Services;

public class SecretCodeMailer
{
    private readonly SmtpSettings _settings;
    private readonly string _loginUrl;

    public SecretCodeMailer(IOptions<SmtpSettings> settings, IConfiguration configuration)
    {
        _settings = settings.Value;
        _loginUrl = BuildLoginUrl(configuration["AppUrl"]);
    }

    public async Task SendSecretCodeAsync(User user, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(user.Email))
        {
            throw new InvalidOperationException("Für diesen Account ist keine E-Mail-Adresse hinterlegt.");
        }

        if (string.IsNullOrWhiteSpace(_settings.Host) ||
            string.IsNullOrWhiteSpace(_settings.FromEmail))
        {
            throw new InvalidOperationException("SMTP ist nicht konfiguriert.");
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_settings.FromEmail, _settings.FromName),
            Subject = "Dein Worktrack Secret Code",
            Body = $"""

Hallo {user.Name},

dein Worktrack Secret Code lautet:
{user.SecretCode}

Du kannst dich damit direkt hier anmelden:
{_loginUrl}

Falls du diese Anfrage nicht selbst gestellt hast, wende dich bitte an den Administrator.
""",
             IsBodyHtml = false
        };

        message.To.Add(user.Email);

        using var client = new SmtpClient(_settings.Host, _settings.Port)
        {
            EnableSsl = _settings.UseSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = _settings.TimeoutMs > 0 ? _settings.TimeoutMs : 15000
        };

        if (!string.IsNullOrWhiteSpace(_settings.Username))
        {
            client.Credentials = new NetworkCredential(_settings.Username, _settings.Password);
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(client.Timeout);

        try
        {
            await client.SendMailAsync(message, timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new TimeoutException($"SMTP-Timeout nach {client.Timeout / 1000} Sekunden.");
        }
    }

    private static string BuildLoginUrl(string? configuredAppUrl)
    {
        var baseUrl = configuredAppUrl?
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault(x => x.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            ?? configuredAppUrl?
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault(x => x.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            ?? "https://clubwork.havelnarren.de";

        return $"{baseUrl.TrimEnd('/')}/user/login";
    }
}
