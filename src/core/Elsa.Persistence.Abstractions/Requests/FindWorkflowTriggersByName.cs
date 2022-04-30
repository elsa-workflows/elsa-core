using Elsa.Helpers;
using Elsa.Mediator.Services;
using Elsa.Persistence.Entities;
using Elsa.Services;

namespace Elsa.Persistence.Requests;

public record FindWorkflowTriggersByName(string Name, string? Hash = default) : IRequest<ICollection<WorkflowTrigger>>
{
    public static FindWorkflowTriggersByName ForTrigger<T>(string? hash = default) where T : IEventGenerator => new(TypeNameHelper.GenerateTypeName<T>(), hash);
}