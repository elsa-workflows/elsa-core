using Elsa.Workflows.Core.Models;

namespace Elsa.Dapper.Modules.Runtime.Records;

internal class WorkflowStateRecord
{
    public string Id { get; set; } = default!;
    public string DefinitionId { get; set; } = default!;
    public int DefinitionVersion { get; set; }
    public string? CorrelationId { get; set; }
    public WorkflowStatus Status { get; set; }
    public WorkflowSubStatus SubStatus { get; set; }
    public string Props { get; set; } = default!;
}