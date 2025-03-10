using Elsa.Mediator.Contracts;

namespace Elsa.Workflows.Runtime.Notifications;

/// <summary>
/// Published when the specified workflow's triggers have been indexed.
/// </summary>
/// <param name="IndexedWorkflowTriggers">Contains the workflow that was indexed and the resulting triggers.</param>
public record WorkflowTriggersIndexed(IndexedWorkflowTriggers IndexedWorkflowTriggers) : INotification;