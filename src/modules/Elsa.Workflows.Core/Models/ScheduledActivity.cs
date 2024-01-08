namespace Elsa.Workflows.Models;

public class ScheduledActivity
{
    public string? ActivityNodeId { get; set; }
    public string? OwnerActivityInstanceId { get; set; }
    public ScheduledActivityOptions? Options { get; set; }
}