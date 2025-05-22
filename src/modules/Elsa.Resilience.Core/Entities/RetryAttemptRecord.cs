using Elsa.Common.Entities;
using Elsa.Workflows.State;

namespace Elsa.Resilience.Entities;

public class RetryAttemptRecord : Entity
{
    public string ActivityInstanceId { get; set; } = null!;
    public string ActivityId { get; set; } = null!;
    public string WorkflowInstanceId { get; set; } = null!;
    public int AttemptNumber { get; set; }
    public TimeSpan RetryDelay { get; set; }
    public string? Result { get; set; }
    public ExceptionState? Exception { get; set; }
}