using Elsa.Api.Client.Shared.Models;

namespace Elsa.Api.Client.Resources.Resilience.Models;

public class RetryAttemptRecord : Entity
{
    public string ActivityInstanceId { get; set; } = null!;
    public string ActivityId { get; set; } = null!;
    public string WorkflowInstanceId { get; set; } = null!;
    public int AttemptNumber { get; set; }
    public TimeSpan RetryDelay { get; set; }
    public IDictionary<string, string?> Details { get; set; } = null!;
}