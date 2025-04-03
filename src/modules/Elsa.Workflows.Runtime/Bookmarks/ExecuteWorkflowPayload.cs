using Elsa.Workflows.Runtime.Activities;

namespace Elsa.Workflows.Runtime.Bookmarks;

/// <summary>
/// Bookmark payload for the <see cref="ExecuteWorkflow"/> activity.
/// </summary>
/// <param name="ChildInstanceId">The instance ID of the child workflow that was created by the <see cref="ExecuteWorkflow"/> activity.</param>
public record ExecuteWorkflowPayload(string ChildInstanceId);