using System.Security.Cryptography;
using System.Text;

namespace Elsa.Secrets.Options;

public class SecretsOptions
{
    public string ConfigurationSectionName { get; set; } = "Elsa:Secrets";
    public byte[] EncryptionKey { get; set; } = SHA256.HashData(Encoding.UTF8.GetBytes("Elsa.Secrets.DevelopmentKey"));
}
