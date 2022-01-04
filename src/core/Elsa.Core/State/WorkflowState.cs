namespace Elsa.State;

public class WorkflowState
{
    public string Id { get; set; } = default!;
    public IDictionary<string, IDictionary<string, object?>> ActivityOutput { get; set; } = new Dictionary<string, IDictionary<string, object?>>();
    public ICollection<CompletionCallbackState> CompletionCallbacks { get; set; } = new List<CompletionCallbackState>();
    public ICollection<ActivityExecutionContextState> ActivityExecutionContexts { get; set; } = new List<ActivityExecutionContextState>();
    public IDictionary<string, object?> Properties { get; set; } = new Dictionary<string, object?>();
}