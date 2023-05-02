using Elsa.Mediator.Contracts;
using JetBrains.Annotations;

namespace Elsa.Workflows.Management.Notifications;

/// <summary>
/// A notification that is sent when a workflow definition version is deleted.
/// </summary>
/// <param name="DefinitionId">The ID of the workflow definition.</param>
/// <param name="Version">The version number of the workflow definition.</param>
[PublicAPI]
public record WorkflowDefinitionVersionDeleted(string DefinitionId, int Version) : INotification;