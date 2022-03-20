using Elsa.Mediator.Contracts;

namespace Elsa.Persistence.Commands;

public record DeleteWorkflowInstance : ICommand<int>
{
    public DeleteWorkflowInstance(string instanceId) => InstanceId = instanceId;
    public string? InstanceId { get; set; }
}