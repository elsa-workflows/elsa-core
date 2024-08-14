using Elsa.Common;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Stimuli;

namespace Elsa.Workflows.Runtime.Bookmarks;

/// <summary>
/// Bookmark payload for the <see cref="DispatchWorkflow"/> activity.
/// </summary>
/// <param name="ChildInstanceId">The instance ID of the child workflow that was created by the <see cref="DispatchWorkflow"/> activity.</param>
[Obsolete("Use DispatchWorkflowStimulus instead.")]
[ForwardedType(typeof(DispatchWorkflowStimulus))]
public record DispatchWorkflowBookmark(string ChildInstanceId);