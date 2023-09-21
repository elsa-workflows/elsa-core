using System.Text.Json;
using Elsa.Mediator.Contracts;

namespace Elsa.Workflows.Core.Notifications;

public record SerializingWorkflowState(JsonSerializerOptions SerializerOptions) : INotification;