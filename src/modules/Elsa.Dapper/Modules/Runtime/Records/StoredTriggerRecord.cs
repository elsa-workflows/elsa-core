namespace Elsa.Dapper.Modules.Runtime.Records;

internal class StoredTriggerRecord
{
    public string Id { get; set; } = default!;
    public string WorkflowDefinitionId { get; set; } = default!;
    public string WorkflowDefinitionVersionId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string ActivityId { get; set; } = default!;
    public string? Hash { get; set; }
    public string? SerializedPayload { get; set; }
}