namespace Elsa.Activities.Conductor.Models
{
    public record RunTaskModel(string Task, object? Payload, string WorkflowInstanceId);
}