using Elsa.Mediator.Contracts;
using JetBrains.Annotations;

namespace Elsa.Workflows.Management.Notifications;

/// <summary>
/// A notification that is sent when a workflow definition is deleted.
/// </summary>
/// <param name="DefinitionId">The ID of the workflow definition.</param>
[PublicAPI]
public record WorkflowDefinitionDeleted(string DefinitionId) : INotification;