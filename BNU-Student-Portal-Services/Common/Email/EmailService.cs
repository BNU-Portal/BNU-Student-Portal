using BNU_Student_Portal_Services_Implementation;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace BNU_Student_Portal_Services.Common.Behaviors.Email;

public class EmailService(
        IOptions<EmailSettings> emailSettings,
        ILogger<EmailService> logger
    ) : IEmailService
{
    /// <summary>
    /// Build the email object → connect to Gmail → authenticate → hand off the email → disconnect.
    /// Logs each step so SMTP failures are visible in the console instead of disappearing silently.
    /// </summary>
    public async Task SendAsync(string to, string subject, string body, bool isHtml = true)
    {
        var settings = emailSettings.Value;
        logger.LogInformation("[Email] Preparing to send '{Subject}' to {To} via {Host}:{Port}",
            subject, to, settings.Host, settings.Port);

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(settings.FromName, settings.FromEmail));
        message.To.Add(new MailboxAddress(to, to));
        message.Subject = subject;
        message.Body = new TextPart(isHtml ? "html" : "plain") { Text = body };

        using var client = new SmtpClient();

        try
        {
            logger.LogInformation("[Email] Connecting to SMTP {Host}:{Port} (StartTls)...",
                settings.Host, settings.Port);

            // SecureSocketOptions.StartTls forces STARTTLS on port 587 explicitly.
            // The old bool overload (UseSsl=false) was ambiguous and could negotiate plain,
            // which Gmail rejects silently.
            await client.ConnectAsync(settings.Host, settings.Port, SecureSocketOptions.StartTls);

            logger.LogInformation("[Email] Connected. Authenticating as {Username}...", settings.Username);
            await client.AuthenticateAsync(settings.Username, settings.Password);

            logger.LogInformation("[Email] Authenticated. Sending message to {To}...", to);
            await client.SendAsync(message);

            logger.LogInformation("[Email] Message sent successfully to {To}.", to);
        }
        catch (AuthenticationException authEx)
        {
            logger.LogError(authEx,
                "[Email] AUTHENTICATION FAILED for {Username}. " +
                "Verify the Gmail App Password is correct and 2FA is enabled on the account.",
                settings.Username);
            throw;
        }
        catch (SmtpCommandException smtpEx)
        {
            logger.LogError(smtpEx,
                "[Email] SMTP COMMAND ERROR — StatusCode: {StatusCode}, Message: {SmtpMessage}",
                smtpEx.StatusCode, smtpEx.Message);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "[Email] Unexpected error sending email to {To}.", to);
            throw;
        }
        finally
        {
            if (client.IsConnected)
                await client.DisconnectAsync(true);
        }
    }
}
