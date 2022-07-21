using Elsa.ActivityDefinitions.Entities;
using Elsa.Mediator.Services;

namespace Elsa.ActivityDefinitions.Notifications;
public record ActivityDefinitionPublished(ActivityDefinition ActivityDefinition) : INotification;