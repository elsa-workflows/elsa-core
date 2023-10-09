using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Results;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Adds extension methods to <see cref="IWorkflowRuntime"/>.
/// </summary>
[PublicAPI]
public static class WorkflowRuntimeExtensions
{
    /// <summary>
    /// Trigger all workflows with bookmarks associated with the specified <see cref="TActivity"/> type.
    /// </summary>
    public static Task<TriggerWorkflowsResult> TriggerWorkflowsAsync<TActivity>(this IWorkflowRuntime workflowRuntime, object bookmarkPayload, TriggerWorkflowsOptions options) where TActivity : IActivity
    {
        return workflowRuntime.TriggerWorkflowsAsync(ActivityTypeNameHelper.GenerateTypeName<TActivity>(), bookmarkPayload, options);
    }
}