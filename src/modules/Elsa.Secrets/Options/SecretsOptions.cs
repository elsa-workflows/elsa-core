namespace Elsa.Secrets.Options;

public class SecretsOptions
{
    public static string DefaultRepositoryFilePath { get; } = Path.Join(AppContext.BaseDirectory, "App_Data", "elsa-secrets.json");

    public string ConfigurationSectionName { get; set; } = "Elsa:Secrets";
    public string? RepositoryFilePath { get; set; }
    public byte[]? EncryptionKey { get; set; }
}
