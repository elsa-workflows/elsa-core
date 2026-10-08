namespace Elsa.Connections.Models;

/// <summary>Maps a tenant-owned logical workflow reference to one connection in one environment.</summary>
public sealed class ConnectionCredentialBinding
{
    public string TenantId { get; set; } = null!;
    public string EnvironmentId { get; set; } = null!;
    public string LogicalBindingId { get; set; } = null!;
    public string ConnectionId { get; set; } = null!;
    public long Revision { get; set; } = 1;
}
