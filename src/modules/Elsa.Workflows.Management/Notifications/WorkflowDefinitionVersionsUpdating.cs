using Elsa.Mediator.Contracts;
using JetBrains.Annotations;

namespace Elsa.Workflows.Management.Notifications;

/// <summary>
/// A notification that is sent when specific workflow definition versions are about to be updated.
/// </summary>
/// <param name="DefinitionsAsActivity">A dictionary of the definition version ID combined with if the workflow is marked as usable as activity.</param>
[PublicAPI]
public record WorkflowDefinitionVersionsUpdating(IDictionary<string, bool> DefinitionsAsActivity) : INotification;