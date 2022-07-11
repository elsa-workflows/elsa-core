using Elsa.CustomActivities.Entities;
using Elsa.Mediator.Services;
using Elsa.Workflows.Persistence.Entities;

namespace Elsa.CustomActivities.Notifications;
public record ActivityDefinitionPublished(ActivityDefinition ActivityDefinition) : INotification;