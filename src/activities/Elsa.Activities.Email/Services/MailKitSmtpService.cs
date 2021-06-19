using System;
using System.IO;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Email.Options;
using Elsa.Services.Models;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Elsa.Activities.Email.Services
{
    public class MailKitSmtpService : ISmtpService
    {
        private readonly SmtpOptions _options;
        private readonly ILogger<MailKitSmtpService> _logger;
        private const string EmailExtension = ".eml";

        public MailKitSmtpService(
            IOptions<SmtpOptions> options,
            ILogger<MailKitSmtpService> logger
        )
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task SendAsync(ActivityExecutionContext context, MimeMessage message, CancellationToken cancellationToken)
        {
            switch (_options.DeliveryMethod)
            {
                case SmtpDeliveryMethod.Network:
                    await SendOnlineMessage(message, cancellationToken);
                    break;
                case SmtpDeliveryMethod.SpecifiedPickupDirectory:
                    await SendOfflineMessage(message, _options.PickupDirectoryLocation!);
                    break;
                default:
                    throw new NotSupportedException($"The '{_options.DeliveryMethod}' delivery method is not supported.");
            }
        }

        private bool CertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            _logger.LogError(
                "SMTP Server's certificate {CertificateSubject} issued by {CertificateIssuer} with thumbprint {CertificateThumbprint} and expiration date {CertificateExpirationDate} is considered invalid with {SslPolicyErrors} policy errors",
                certificate.Subject,
                certificate.Issuer,
                certificate.GetCertHashString(),
                certificate.GetExpirationDateString(),
                sslPolicyErrors);

            if (sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateChainErrors) && chain?.ChainStatus != null)
                foreach (var chainStatus in chain.ChainStatus)
                    _logger.LogError("Status: {Status} - {StatusInformation}", chainStatus.Status, chainStatus.StatusInformation);

            return false;
        }

        private async Task SendOnlineMessage(MimeMessage message, CancellationToken cancellationToken)
        {
            using (var client = new SmtpClient())
            {
                client.ServerCertificateValidationCallback = CertificateValidationCallback;

                await client.ConnectAsync(_options.Host, _options.Port, _options.SecureSocketOptions, cancellationToken);
                
                if (_options.RequireCredentials)
                {
                    if (_options.UseDefaultCredentials)
                    {
                        // There's no notion of 'UseDefaultCredentials' in MailKit, so empty credentials are passed in.
                        await client.AuthenticateAsync(string.Empty, string.Empty, cancellationToken);
                    }
                    else if (!string.IsNullOrWhiteSpace(_options.UserName))
                    {
                        await client.AuthenticateAsync(_options.UserName, _options.Password, cancellationToken);
                    }
                }
                
                await client.SendAsync(message, cancellationToken);
                await client.DisconnectAsync(true, cancellationToken);
            }
        }

        private async Task SendOfflineMessage(MimeMessage message, string pickupDirectory)
        {
            var mailPath = Path.Combine(pickupDirectory, Guid.NewGuid() + EmailExtension);
            await message.WriteToAsync(mailPath, CancellationToken.None);
        }
    }
}