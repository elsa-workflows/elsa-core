using System.Text.Json;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Services;

namespace Elsa.Workflows.Notifications;

/// <summary>
/// A notification that is published when a value is being serialized by <see cref="SafeSerializer"/>.
/// </summary>
public record SafeSerializerSerializing(object? Value, JsonSerializerOptions SerializerOptions) : INotification;