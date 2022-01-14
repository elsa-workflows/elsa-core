using Elsa.Contracts;
using Elsa.Helpers;
using Elsa.Mediator.Contracts;
using Elsa.Persistence.Entities;

namespace Elsa.Persistence.Requests;

public record FindWorkflowTriggers : IRequest<IEnumerable<WorkflowTrigger>>
{
    public FindWorkflowTriggers(string name, string? hash = default)
    {
        Name = name;
        Hash = hash;
    }

    public string Name { get; }
    public string? Hash { get; }

    public static FindWorkflowTriggers ForTrigger<T>(string? hash = default) where T : ITrigger => new(TypeNameHelper.GenerateTypeName<T>(), hash);
}