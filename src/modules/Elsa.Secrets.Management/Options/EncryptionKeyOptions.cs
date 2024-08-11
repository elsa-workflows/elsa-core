namespace Elsa.Secrets.Management;

public class EncryptionKeyOptions
{
    public ICollection<EncryptionKey> EncryptionKeys { get; set; } = new List<EncryptionKey>();
}