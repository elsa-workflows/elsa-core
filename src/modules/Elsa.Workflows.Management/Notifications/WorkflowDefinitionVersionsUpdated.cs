using Elsa.Mediator.Contracts;
using JetBrains.Annotations;

namespace Elsa.Workflows.Management.Notifications;

/// <summary>
/// A notification that is sent when specific workflow definition versions are updated.
/// </summary>
/// <param name="DefinitionsAsActivity">A dictionary of Ids combined with if the workflow definition is marked usable as activity.</param>
[PublicAPI]
public record WorkflowDefinitionVersionsUpdated(IDictionary<string, bool> DefinitionsAsActivity) : INotification;