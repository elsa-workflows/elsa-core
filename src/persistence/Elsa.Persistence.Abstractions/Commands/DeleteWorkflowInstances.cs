using System.Collections.Generic;
using System.Linq;
using Elsa.Mediator.Contracts;

namespace Elsa.Persistence.Commands;

/// <summary>
/// Represents a command to delete all instances of the specified <see cref="DefinitionId"/> or having one of the specified <see cref="InstanceIds"/>.
/// </summary>
public record DeleteWorkflowInstances : ICommand<int>
{
    public DeleteWorkflowInstances(string definitionId) => DefinitionId = definitionId;
    public DeleteWorkflowInstances(IEnumerable<string> instanceIds) => InstanceIds = instanceIds.ToList();

    public string? DefinitionId { get; set; }
    public ICollection<string>? InstanceIds { get; set; }
}