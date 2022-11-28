using MimeKit;

namespace Elsa.Email.Services;

/// <summary>
/// Use this service to send emails.
/// </summary>
public interface ISmtpService
{
    /// <summary>
    /// Send the specified <see cref="MimeMessage"/>.
    /// </summary>
    Task SendAsync(MimeMessage message, CancellationToken cancellationToken);
}