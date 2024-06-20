using Elsa.Mediator.Contracts;
using JetBrains.Annotations;

namespace Elsa.Workflows.Management.Notifications;

/// <summary>
/// A notification that is sent when specific workflow definition versions are updated.
/// </summary>
[PublicAPI]
public record WorkflowDefinitionVersionsUpdated(IEnumerable<WorkflowDefinitionVersionUpdate> VersionUpdates) : INotification;