using System.Collections.Generic;
using Elsa.Mediator.Contracts;
using Elsa.Persistence.Entities;

namespace Elsa.Persistence.Requests;

public record FindWorkflowTriggers : IRequest<IEnumerable<WorkflowTrigger>>
{
    public FindWorkflowTriggers(string name, string? hash)
    {
        Name = name;
        Hash = hash;
    }
    
    public string Name { get; }
    public string? Hash { get; }
}