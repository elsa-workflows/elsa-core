using Elsa.Mediator.Contracts;

namespace Elsa.Persistence.Commands;

/// <summary>
/// Represents a command to delete all versions of the specified definition ID. 
/// </summary>
public record DeleteWorkflowDefinition(string DefinitionId) : ICommand<bool>;