using Elsa.Mediator.Services;

namespace Elsa.Workflows.Runtime.Models;

/// <summary>
/// Published when the all workflows' triggers have been indexed.
/// </summary>
/// <param name="IndexedWorkflows">Contains the workflows that were indexed and their resulting triggers.</param>
public record WorkflowIndexingCompleted(ICollection<IndexedWorkflowTriggers> IndexedWorkflows) : INotification;