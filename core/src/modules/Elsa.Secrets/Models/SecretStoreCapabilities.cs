namespace Elsa.Secrets.Models;

[Flags]
public enum SecretStoreCapabilities
{
    None = 0,
    Read = 1,
    Write = 2,
    Delete = 4,
    Test = 8,
    ExportEncrypted = 16,
    Versioned = 32
}
