using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Runtime.Notifications;

/// <summary>
/// An event that is published when activity executions are deleted.
/// </summary>
/// <param name="Record">The activity execution record.</param>
public record ActivityExecutionRecordDeleted(ActivityExecutionRecord Record) : INotification;