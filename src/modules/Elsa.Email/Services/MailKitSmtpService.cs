using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Elsa.Email.Contracts;
using Elsa.Email.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace Elsa.Email.Services;

/// <summary>
/// A MailKit implementation of <see cref="ISmtpService"/>.
/// </summary>
public class MailKitSmtpService : ISmtpService
{
    private readonly SmtpOptions _options;
    private readonly ILogger<MailKitSmtpService> _logger;

    /// <summary>
    /// Constructor.
    /// </summary>
    public MailKitSmtpService(
        IOptions<SmtpOptions> options,
        ILogger<MailKitSmtpService> logger
    )
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Sends the specified message.
    /// </summary>
    public async Task SendAsync(MimeMessage message, CancellationToken cancellationToken) => await SendMessage(message, cancellationToken);
    
    private async Task SendMessage(MimeMessage message, CancellationToken cancellationToken)
    {
        using var client = new SmtpClient();
        client.ServerCertificateValidationCallback = CertificateValidationCallback;

        await client.ConnectAsync(_options.Host, _options.Port, _options.SecureSocketOptions, cancellationToken);
                
        if (_options.RequireCredentials) 
            await client.AuthenticateAsync(_options.UserName ?? "", _options.Password ?? "", cancellationToken);

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }

    private bool CertificateValidationCallback(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
    {
        if (sslPolicyErrors == SslPolicyErrors.None)
            return true;

        _logger.LogError(
            "SMTP Server's certificate {CertificateSubject} issued by {CertificateIssuer} with thumbprint {CertificateThumbprint} and expiration date {CertificateExpirationDate} is considered invalid with {SslPolicyErrors} policy errors",
            certificate?.Subject,
            certificate?.Issuer,
            certificate?.GetCertHashString(),
            certificate?.GetExpirationDateString(),
            sslPolicyErrors);

        if (!sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateChainErrors) || chain?.ChainStatus == null) 
            return false;
        
        foreach (var chainStatus in chain.ChainStatus)
            _logger.LogError("Status: {Status} - {StatusInformation}", chainStatus.Status, chainStatus.StatusInformation);

        return false;
    }

}