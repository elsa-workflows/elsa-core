using Elsa.Mediator.Services;

namespace Elsa.CustomActivities.Notifications;

public record ActivityDefinitionsDeleted(ICollection<string> DefinitionIds) : INotification;