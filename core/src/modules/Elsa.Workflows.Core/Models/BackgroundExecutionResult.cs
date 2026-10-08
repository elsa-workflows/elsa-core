namespace Elsa.Workflows.Models;

public class BackgroundExecutionResult
{
    public ICollection<BackgroundExecutionOutcome> Outcomes { get; set; } = new List<BackgroundExecutionOutcome>();
    public ICollection<WorkflowExecutionLogEntry> ExecutionLog { get; set; } = new List<WorkflowExecutionLogEntry>();
    public IDictionary<string, object?> JournalData { get; } = new Dictionary<string, object?>();
}