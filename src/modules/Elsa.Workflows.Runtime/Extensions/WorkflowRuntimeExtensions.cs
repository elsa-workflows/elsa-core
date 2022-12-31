using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Runtime.Services;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Adds extension methods to <see cref="IWorkflowRuntime"/>.
/// </summary>
public static class WorkflowRuntimeExtensions
{
    /// <summary>
    /// Trigger all workflows with bookmarks associated with the specified <see cref="TActivity"/> type.
    /// </summary>
    public static Task<TriggerWorkflowsResult> TriggerWorkflowsAsync<TActivity>(this IWorkflowRuntime workflowRuntime, object bookmarkPayload, TriggerWorkflowsRuntimeOptions options, CancellationToken cancellationToken = default) where TActivity : IActivity =>
        workflowRuntime.TriggerWorkflowsAsync(ActivityTypeNameHelper.GenerateTypeName<TActivity>(), bookmarkPayload, options, cancellationToken);
}