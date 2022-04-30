using Elsa.Mediator.Services;
using Elsa.Models;
using Elsa.Persistence.Entities;

namespace Elsa.Persistence.Commands;

/// <summary>
/// Represents a command to persist the specified <see cref="WorkflowDefinition"/> to storage.
/// </summary>
public record SaveWorkflowDefinition(WorkflowDefinition WorkflowDefinition) : ICommand;