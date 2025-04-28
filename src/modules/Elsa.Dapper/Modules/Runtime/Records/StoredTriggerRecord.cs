using Elsa.Dapper.Records;

namespace Elsa.Dapper.Modules.Runtime.Records;

internal class StoredTriggerRecord : Record
{
    public string WorkflowDefinitionId { get; set; } = null!;
    public string WorkflowDefinitionVersionId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string ActivityId { get; set; } = null!;
    public string? Hash { get; set; }
    public string? SerializedPayload { get; set; }
}