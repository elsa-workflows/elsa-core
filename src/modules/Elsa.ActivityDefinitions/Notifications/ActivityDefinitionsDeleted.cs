using Elsa.Mediator.Services;

namespace Elsa.ActivityDefinitions.Notifications;

public record ActivityDefinitionsDeleted(ICollection<string> DefinitionIds) : INotification;