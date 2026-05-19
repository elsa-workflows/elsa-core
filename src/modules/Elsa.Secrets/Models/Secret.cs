namespace Elsa.Secrets.Models;

public class Secret
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public string? Description { get; set; }
    public string TypeName { get; set; } = SecretTypeNames.Text;
    public string StoreName { get; set; } = SecretStoreNames.Encrypted;
    public string? Scope { get; set; }
    [System.Text.Json.Serialization.JsonConverter(typeof(CaseInsensitiveHashSetConverter))]
    public HashSet<string> Tags { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public SecretStatus Status { get; set; } = SecretStatus.Active;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public IList<SecretVersion> Versions { get; set; } = [];

    public SecretVersion? LatestActiveVersion => Versions
        .Where(x => x.Status == SecretStatus.Active && !x.IsExpired())
        .OrderByDescending(x => x.Version)
        .FirstOrDefault();
}
