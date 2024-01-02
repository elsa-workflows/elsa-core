using System.Text.Json;
using Elsa.Mediator.Contracts;

namespace Elsa.Workflows.Notifications;

/// <summary>
/// Triggered when an object is being serialized.
/// </summary>
public record SafeSerializerDeserializing(JsonSerializerOptions Options) : INotification;