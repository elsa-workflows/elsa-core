using Elsa.MassTransit.Messages;

namespace Elsa.MassTransit.Contracts;

/// <summary>
/// Dispatches workflow definition related notifications to refresh the activity registry.
/// </summary>
public interface IDistributedWorkflowDefinitionEventsDispatcher
{
    /// <summary>
    /// Dispatches a workflow definition publication event.
    /// </summary>
    Task DispatchAsync(WorkflowDefinitionPublished request, CancellationToken cancellationToken);

    /// <summary>
    /// Dispatches a workflow definition retracted event.
    /// </summary>
    Task DispatchAsync(WorkflowDefinitionRetracted request, CancellationToken cancellationToken);

    /// <summary>
    /// Dispatches a workflow definition deleted event.
    /// </summary>
    Task DispatchAsync(WorkflowDefinitionDeleted request, CancellationToken cancellationToken);

    /// <summary>
    /// Dispatches a workflow definitions deleted event.
    /// </summary>
    Task DispatchAsync(WorkflowDefinitionsDeleted request, CancellationToken cancellationToken);

    /// <summary>
    /// Dispatches a workflow definition created event.
    /// </summary>
    Task DispatchAsync(WorkflowDefinitionCreated request, CancellationToken cancellationToken);

    /// <summary>
    /// Dispatches a workflow definition version deleted event.
    /// </summary>
    Task DispatchAsync(WorkflowDefinitionVersionDeleted request, CancellationToken cancellationToken);

    /// <summary>
    /// Dispatches a workflow definition versions deleted event.
    /// </summary>
    Task DispatchAsync(WorkflowDefinitionVersionsDeleted request, CancellationToken cancellationToken);
}