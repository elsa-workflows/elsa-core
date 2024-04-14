using Elsa.Workflows.Models;

namespace Elsa.Workflows.Runtime.Distributed.Messages;

public record RunWorkflowInstanceResponse
{
    public WorkflowStatus Status { get; set; }
    public WorkflowSubStatus SubStatus { get; set; }
    public ICollection<ActivityIncident> Incidents { get; set; } = new List<ActivityIncident>();
}