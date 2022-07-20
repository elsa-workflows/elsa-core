using Elsa.Mediator.Services;

namespace Elsa.ActivityDefinitions.Notifications;

public record ActivityDefinitionDeleted(string DefinitionId) : INotification;