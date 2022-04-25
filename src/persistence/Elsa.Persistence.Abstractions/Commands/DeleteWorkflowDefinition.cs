using Elsa.Mediator.Services;

namespace Elsa.Persistence.Commands;

/// <summary>
/// Represents a command to delete all versions of the specified definition ID. 
/// </summary>
public record DeleteWorkflowDefinition(string DefinitionId) : ICommand<bool>;