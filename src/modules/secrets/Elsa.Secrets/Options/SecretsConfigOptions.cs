namespace Elsa.Secrets.Options;

public class SecretsConfigOptions
{
    public bool? Enabled { get; set; }
    public bool? EncryptionEnabled { get; set; }
    public string? EncryptionKey { get; set; }
    public string[]? EncryptedProperties { get; set; }
    public string? ConnectionStringIdentifier { get; set; }
    public string? ConnectionString { get; set; }
}