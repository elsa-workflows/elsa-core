namespace Elsa.Secrets.Models;

public static class SecretStoreNames
{
    public const string Encrypted = "encrypted";
    public const string Configuration = "configuration";
}

public static class SecretTypeNames
{
    public const string Text = "text";
    public const string RsaKey = "rsa-key";
    public const string X509Certificate = "x509-certificate";
}

public record SecretStoreDescriptor(
    string Name,
    string DisplayName,
    string Description,
    SecretStoreCapabilities Capabilities,
    bool IsReadOnly);

public record SecretTypeDescriptor(
    string Name,
    string DisplayName,
    string Description,
    string EditorHint,
    IReadOnlyCollection<string> SupportedStoreNames);
