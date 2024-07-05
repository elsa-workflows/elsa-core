using Elsa.Mediator.Contracts;

namespace Elsa.Workflows.Runtime.Notifications;

/// Published when the specified workflow's triggers have been indexed.
/// <param name="IndexedWorkflowTriggers">Contains the workflow that was indexed and the resulting triggers.</param>
public record WorkflowTriggersIndexed(IndexedWorkflowTriggers IndexedWorkflowTriggers) : INotification;