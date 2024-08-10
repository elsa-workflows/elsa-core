namespace Elsa.Secrets.Management;

public class SecretFilter
{
    public string? Id { get; set; }
    public ICollection<string>? Ids { get; set; }
    public string? Name { get; set; }
    public int? Version { get; set; }
    public string? EncryptionKeyId { get; set; }
    public string? SearchTerm { get; set; }
}