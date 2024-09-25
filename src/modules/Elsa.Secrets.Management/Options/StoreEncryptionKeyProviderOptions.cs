using System.Security.Cryptography;

namespace Elsa.Secrets.Management;

public class EncryptionKeyProviderOptions
{
    public string Key { get; set; } = default!;
    public string IV { get; set; } = default!;
    public string Algorithm { get; set; } = nameof(Aes);
}