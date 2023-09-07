namespace Elsa.Workflows.Runtime.Requests;

public class TriggerWorkflowsOptions
{
    public TriggerWorkflowsOptions(string? CorrelationId = default, string? WorkflowInstanceId = default, string? ActivityInstanceId = default, IDictionary<string, object>? Input = default)
    {
        this.CorrelationId = CorrelationId;
        this.WorkflowInstanceId = WorkflowInstanceId;
        this.ActivityInstanceId = ActivityInstanceId;
        this.Input = Input;
    }

    public string? CorrelationId { get; set; }
    public string? WorkflowInstanceId { get; set; }
    public string? ActivityInstanceId { get; set; }
    public IDictionary<string, object>? Input { get; set; }
}