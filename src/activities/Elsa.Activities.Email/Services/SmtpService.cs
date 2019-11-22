using System;
using System.IO;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Email.Options;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Elsa.Activities.Email.Services
{
    public class SmtpService : ISmtpService
    {
        private readonly SmtpOptions options;
        private readonly ILogger<SmtpService> logger;
        private const string EmailExtension = ".eml";

        public SmtpService(
            IOptions<SmtpOptions> options,
            ILogger<SmtpService> logger
        )
        {
            this.options = options.Value;
            this.logger = logger;
        }

        public async Task SendAsync(MimeMessage message, CancellationToken cancellationToken)
        {
            switch (options.DeliveryMethod)
            {
                case SmtpDeliveryMethod.Network:
                    await SendOnlineMessage(message, cancellationToken);
                    break;
                case SmtpDeliveryMethod.SpecifiedPickupDirectory:
                    await SendOfflineMessage(message, options.PickupDirectoryLocation);
                    break;
                default:
                    throw new NotSupportedException($"The '{options.DeliveryMethod}' delivery method is not supported.");
            }
        }

        private bool CertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            logger.LogError(
                "SMTP Server's certificate {CertificateSubject} issued by {CertificateIssuer} with thumbprint {CertificateThumbprint} and expiration date {CertificateExpirationDate} is considered invalid with {SslPolicyErrors} policy errors",
                certificate.Subject,
                certificate.Issuer,
                certificate.GetCertHashString(),
                certificate.GetExpirationDateString(),
                sslPolicyErrors);

            if (sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateChainErrors) && chain?.ChainStatus != null)
                foreach (var chainStatus in chain.ChainStatus)
                    logger.LogError("Status: {Status} - {StatusInformation}", chainStatus.Status, chainStatus.StatusInformation);

            return false;
        }

        private async Task SendOnlineMessage(MimeMessage message, CancellationToken cancellationToken)
        {
            using (var client = new SmtpClient())
            {
                client.ServerCertificateValidationCallback = CertificateValidationCallback;

                await client.ConnectAsync(options.Host, options.Port, options.SecureSocketOptions, cancellationToken);
                
                if (options.RequireCredentials)
                {
                    if (options.UseDefaultCredentials)
                    {
                        // There's no notion of 'UseDefaultCredentials' in MailKit, so empty credentials are passed in.
                        await client.AuthenticateAsync(string.Empty, string.Empty, cancellationToken);
                    }
                    else if (!string.IsNullOrWhiteSpace(options.UserName))
                    {
                        await client.AuthenticateAsync(options.UserName, options.Password, cancellationToken);
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