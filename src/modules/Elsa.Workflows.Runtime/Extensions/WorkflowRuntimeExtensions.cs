using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.Workflows.Runtime.Extensions;

public static class WorkflowRuntimeExtensions
{
    public static Task<TriggerWorkflowsResult> TriggerWorkflowsAsync<TActivity>(this IWorkflowRuntime workflowRuntime, object bookmarkPayload, TriggerWorkflowsRuntimeOptions options, CancellationToken cancellationToken = default) where TActivity : IActivity =>
        workflowRuntime.TriggerWorkflowsAsync(ActivityTypeNameHelper.GenerateTypeName<TActivity>(), bookmarkPayload, options, cancellationToken);
}