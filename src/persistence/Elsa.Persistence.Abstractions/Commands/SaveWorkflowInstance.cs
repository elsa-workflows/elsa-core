using Elsa.Mediator.Contracts;
using Elsa.Persistence.Entities;

namespace Elsa.Persistence.Commands;

/// <summary>
/// Represents a command to persist the specified <see cref="WorkflowInstance"/> to storage.
/// </summary>
public record SaveWorkflowInstance(WorkflowInstance WorkflowInstance) : ICommand;