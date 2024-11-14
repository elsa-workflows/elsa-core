using Elsa.Workflows.Activities;

namespace Elsa.Workflows.Runtime;

public class InvokeTriggerRequest
{
    public Workflow Workflow { get; set; } = default!;
    public string ActivityId { get; set; } = default!;
    public string? CorrelationId { get; set; }
    public IDictionary<string, object>? Input { get; set; }
    public IDictionary<string, object>? Properties { get; set; }
    public string? ParentWorkflowInstanceId { get; set; }
    
}