using Elsa.Mediator.Services;

namespace Elsa.ActivityDefinitions.Notifications;

public record ActivityDefinitionVersionDeleted(string DefinitionId, int Version) : INotification;