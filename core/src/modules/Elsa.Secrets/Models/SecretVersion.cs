namespace Elsa.Secrets.Models;

public class SecretVersion
{
    public int Version { get; set; }
    public SecretStatus Status { get; set; } = SecretStatus.Active;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresAt { get; set; }
    public SecretPayload Payload { get; set; } = new();

    public bool IsExpired() => ExpiresAt <= DateTimeOffset.UtcNow || Status == SecretStatus.Expired;
}
