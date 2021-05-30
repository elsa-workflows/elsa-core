namespace Elsa.Activities.Conductor.Models
{
    public record SendCommandModel(string Command, object? Payload, string WorkflowInstanceId);
}