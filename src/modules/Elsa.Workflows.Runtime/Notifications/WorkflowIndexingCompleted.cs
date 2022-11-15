using System.Collections.Generic;
using Elsa.Mediator.Services;
using Elsa.Workflows.Runtime.Models;

namespace Elsa.Workflows.Runtime.Notifications;

/// <summary>
/// Published when the all workflows' triggers have been indexed.
/// </summary>
/// <param name="IndexedWorkflows">Contains the workflows that were indexed and their resulting triggers.</param>
public record WorkflowIndexingCompleted(ICollection<IndexedWorkflowTriggers> IndexedWorkflows) : INotification;