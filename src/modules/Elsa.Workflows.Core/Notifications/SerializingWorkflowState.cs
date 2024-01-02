using System.Text.Json;
using Elsa.Mediator.Contracts;

namespace Elsa.Workflows.Notifications;

/// <summary>
/// A notification that is published when a workflow state is being serialized.
/// </summary>
public record SerializingWorkflowState(JsonSerializerOptions SerializerOptions) : INotification;