using Elsa.Dapper.Records;

namespace Elsa.Dapper.Modules.Management.Records;

internal class WorkflowInstanceRecord : Record
{
    public string DefinitionId { get; set; } = null!;
    public string DefinitionVersionId { get; set; } = null!;
    public int Version { get; set; }
    public string WorkflowState { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string SubStatus { get; set; } = null!;
    public bool IsExecuting { get; set; }
    public string? CorrelationId { get; set; }
    public string? Name { get; set; }
    public int IncidentCount { get; set; }
    public bool IsSystem { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
}