using Elsa.Agents.Persistence.Entities;
using Elsa.Mediator.Contracts;

namespace Elsa.Agents.Persistence.Notifications;

public record AgentDefinitionUpdated(AgentDefinition AgentDefinition) : INotification;