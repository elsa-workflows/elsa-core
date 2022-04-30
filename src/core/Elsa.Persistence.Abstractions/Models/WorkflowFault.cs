namespace Elsa.Persistence.Models
{
    public record WorkflowFault(SimpleException? Exception, string Message, string? FaultedActivityId, object? ActivityInput, bool Resuming);
}