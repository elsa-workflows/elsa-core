using Elsa.Extensions;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Parameters;
using Elsa.Workflows.Runtime.Requests;
using Quartz;

namespace Elsa.Quartz.Jobs;

/// <summary>
/// A job that resumes a workflow.
/// </summary>
public class ResumeWorkflowJob(IWorkflowRuntime workflowRuntime) : IJob
{
    /// <summary>
    /// The job key.
    /// </summary>
    public static readonly JobKey JobKey = new(nameof(ResumeWorkflowJob));

    /// <inheritdoc />
    public async Task Execute(IJobExecutionContext context)
    {
        var map = context.MergedJobDataMap;
        var workflowInstanceId = (string)map.Get(nameof(DispatchWorkflowInstanceRequest.InstanceId));
        var options = new ResumeWorkflowRuntimeParams()
        {
            BookmarkId = (string)map.Get(nameof(DispatchWorkflowInstanceRequest.BookmarkId)),
            ActivityId = (string?)map.Get(nameof(DispatchWorkflowInstanceRequest.ActivityId)),
            ActivityNodeId = (string?)map.Get(nameof(DispatchWorkflowInstanceRequest.ActivityNodeId)),
            ActivityInstanceId = (string?)map.Get(nameof(DispatchWorkflowInstanceRequest.ActivityInstanceId)),
            CorrelationId = (string?)map.Get(nameof(DispatchWorkflowInstanceRequest.CorrelationId)),
            Input = map.GetDictionary(nameof(DispatchWorkflowInstanceRequest.Input)),
            CancellationTokens = context.CancellationToken
        };
        await workflowRuntime.ResumeWorkflowAsync(workflowInstanceId, options);
    }
}