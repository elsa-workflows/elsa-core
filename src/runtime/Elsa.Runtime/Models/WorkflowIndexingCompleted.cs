using Elsa.Mediator.Contracts;
using Elsa.Runtime.Notifications;

namespace Elsa.Runtime.Models;

/// <summary>
/// Published when the all workflows' triggers have been indexed.
/// </summary>
/// <param name="IndexedWorkflows">Contains the workflows that were indexed and their resulting triggers.</param>
public record WorkflowIndexingCompleted(ICollection<IndexedWorkflowTriggers> IndexedWorkflows) : INotification;