namespace Elsa.Models
{
    public record WorkflowFault(string? FaultedActivityId, string? Message, string? StackTrace);
}