namespace Elsa.Models
{
    public record WorkflowFault(string? FaultedActivityId, string? Message, string? StackTrace, object? ActivityInput, bool Resuming);
}