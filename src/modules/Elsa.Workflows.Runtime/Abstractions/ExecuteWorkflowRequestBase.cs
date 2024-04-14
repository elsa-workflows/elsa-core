namespace Elsa.Workflows.Runtime.Abstractions;

public abstract class ExecuteWorkflowRequestBase
{
    public string DefinitionVersionId { get; set; }
    public string? CorrelationId { get; set; }
    public IDictionary<string, object>? Input { get; set; }
    public IDictionary<string, object>? Properties { get; set; }
    public string? InstanceId { get; set; }
    public bool? IsExistingInstance { get; set; }
}