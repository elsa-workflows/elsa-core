using Elsa.Mediator.Contracts;
using Elsa.Models;

namespace Elsa.Persistence.Commands;

/// <summary>
/// Represents a command to persist the specified <see cref="Workflow"/> to storage.
/// </summary>
public record SaveWorkflow(Workflow Workflow) : ICommand;