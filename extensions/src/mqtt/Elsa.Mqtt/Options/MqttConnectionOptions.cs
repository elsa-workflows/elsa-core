using System.Security.Cryptography.X509Certificates;
using MQTTnet;

namespace Elsa.Mqtt.Options;

public class MqttConnectionOptions
{
    public required string Host { get; set; }
    public required int Port { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? ClientId { get; set; }

    /// <summary>Enable TLS/SSL for this connection.</summary>
    public bool UseTls { get; set; }

    /// <summary>
    /// Path to a PFX/P12 client certificate file used for mutual TLS authentication.
    /// Ignored when <see cref="UseTls"/> is <c>false</c>.
    /// </summary>
    public string? ClientCertificatePath { get; set; }

    /// <summary>
    /// Password for the client certificate file referenced by <see cref="ClientCertificatePath"/>.
    /// </summary>
    public string? ClientCertificatePassword { get; set; }

    /// <summary>
    /// When <c>true</c>, the broker's certificate is accepted even if it cannot be
    /// validated against a trusted root. Use only in development/testing environments.
    /// </summary>
    public bool AllowUntrustedCertificates { get; set; }

    public MqttClientOptions GenerateMqttClientOptions()
    {
        var mqttClientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(Host, Port)
            .WithKeepAlivePeriod(TimeSpan.FromSeconds(30))
            .WithTimeout(TimeSpan.FromSeconds(30))
            .WithClientId(ClientId ?? $"Elsa{Guid.NewGuid():N}");

        if (!string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(Password))
        {
            mqttClientOptions.WithCredentials(Username, Password);
        }
        else if (!string.IsNullOrEmpty(Username))
        {
            mqttClientOptions.WithCredentials(Username);
        }

        if (UseTls)
        {
            mqttClientOptions.WithTlsOptions(tls =>
            {
                tls.UseTls(true);
                tls.WithAllowUntrustedCertificates(AllowUntrustedCertificates);

                if (!string.IsNullOrEmpty(ClientCertificatePath))
                {
#if NET9_0_OR_GREATER
                    var cert = string.IsNullOrEmpty(ClientCertificatePassword)
                        ? X509CertificateLoader.LoadCertificateFromFile(path: ClientCertificatePath)
                        : X509CertificateLoader.LoadPkcs12FromFile(path: ClientCertificatePath, password: ClientCertificatePassword);
#else
                    var cert = string.IsNullOrEmpty(ClientCertificatePassword)
                        ? new X509Certificate2(fileName: ClientCertificatePath)
                        : new X509Certificate2(fileName: ClientCertificatePath, password: ClientCertificatePassword);
#endif

                    tls.WithClientCertificates([cert]);
                }
            });
        }

        return mqttClientOptions.Build();
    }
}
