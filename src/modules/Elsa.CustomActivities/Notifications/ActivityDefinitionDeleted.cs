using Elsa.Mediator.Services;

namespace Elsa.CustomActivities.Notifications;

public record ActivityDefinitionDeleted(string DefinitionId) : INotification;