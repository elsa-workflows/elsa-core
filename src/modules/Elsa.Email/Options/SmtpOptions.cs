using MailKit.Security;

namespace Elsa.Email.Options;

/// <summary>
/// Options to configure the SMTP client with.
/// </summary>
public class SmtpOptions
{
    /// <summary>
    /// The default sender address when no sender is specified while sending emails.
    /// </summary>
    public string DefaultSender { get; set; } = default!;
    
    /// <summary>
    /// The SMTP server IP address or hostname.
    /// </summary>
    public string? Host { get; set; }
    
    /// <summary>
    /// The SMTP server port
    /// </summary>
    public int Port { get; set; } = 25;
    
    /// <summary>
    /// Secure socket options.
    /// </summary>
    public SecureSocketOptions SecureSocketOptions { get; set; } = SecureSocketOptions.Auto;
    
    /// <summary>
    /// True if the SMTP host requires credentials.
    /// </summary>
    public bool RequireCredentials { get; set; }
    
    /// <summary>
    /// The username to authenticate with.
    /// </summary>
    public string? UserName { get; set; }
    
    /// <summary>
    /// The password to authenticate with.
    /// </summary>
    public string? Password { get; set; }
}