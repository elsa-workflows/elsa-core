namespace Elsa.Email.Options;

/// <summary>
/// Represents the encryption method to use when connecting to an SMTP server.
/// </summary>
public enum SmtpEncryptionMethod
{
    /// <summary>
    /// No encryption.
    /// </summary>
    None = 0,
    
    /// <summary>
    /// SSL/TLS encryption.
    /// </summary>
    SslTlS = 1,
    
    /// <summary>
    /// STARTTLS encryption.
    /// </summary>
    StartTls = 2
}