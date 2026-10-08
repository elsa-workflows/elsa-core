namespace Elsa.Connections.Models;

/// <summary>
/// Authorizes one workflow instance to use one revision of a logical connection binding.
/// It contains no secret material and is not a substitute for the connection lifecycle status check.
/// </summary>
public sealed class ConnectionCredentialUseGrant
{
    public string TenantId { get; set; } = null!;
    public string EnvironmentId { get; set; } = null!;
    public string WorkflowInstanceId { get; set; } = null!;
    public string LogicalBindingId { get; set; } = null!;
    public string ConnectionId { get; set; } = null!;
    public long BindingRevision { get; set; }
    public long Revision { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public string IssuedByActorId { get; set; } = null!;
    public DateTimeOffset IssuedAt { get; set; }
    public DateTimeOffset? WithdrawnAt { get; set; }
}
