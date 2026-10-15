namespace Elsa.Connections.Models;

/// <summary>
/// Durable tombstone for a retired credential generation. Keeping the identity after deletion prevents a
/// generation from being staged or published again after cleanup.
/// </summary>
public sealed class ConnectionGenerationCleanup
{
    public string ConnectionId { get; set; } = default!;
    public string TenantId { get; set; } = default!;
    public string EnvironmentId { get; set; } = default!;
    public string GenerationId { get; set; } = default!;
    public ConnectionGenerationCleanupStatus Status { get; set; }
    public long Fence { get; set; }
    public DateTimeOffset? LeaseExpiresAt { get; set; }
}

public enum ConnectionGenerationCleanupStatus
{
    Deleting,
    Deleted
}
