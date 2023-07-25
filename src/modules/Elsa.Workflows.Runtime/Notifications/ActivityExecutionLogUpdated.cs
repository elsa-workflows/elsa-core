using Elsa.Mediator.Contracts;
using Elsa.Workflows.Core;
using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Runtime.Notifications;

/// <summary>
/// An event that is published when activity executions are persisted.
/// </summary>
/// <param name="WorkflowExecutionContext">The workflow execution context.</param>
/// <param name="Records">The activity execution records.</param>
public record ActivityExecutionLogUpdated(WorkflowExecutionContext WorkflowExecutionContext, List<ActivityExecutionRecord> Records) : INotification;