namespace Elsa.Workflows.Runtime.Parameters;

public abstract class ExecuteWorkflowParamsCommonBase
{
    public string? CorrelationId { get; set; }
    public IDictionary<string, object>? Input { get; set; }
    public IDictionary<string, object>? Properties { get; set; }
}