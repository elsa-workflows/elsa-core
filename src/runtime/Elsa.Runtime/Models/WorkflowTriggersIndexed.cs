using Elsa.Mediator.Contracts;
using Elsa.Runtime.Notifications;

namespace Elsa.Runtime.Models;

/// <summary>
/// Published when the specified workflow's triggers have been indexed.
/// </summary>
/// <param name="IndexedWorkflow">Contains the workflow that was indexed and the resulting triggers.</param>
public record WorkflowTriggersIndexed(IndexedWorkflow IndexedWorkflow) : INotification;