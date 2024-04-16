using Elsa.Common.Contracts;
using Elsa.Extensions;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Requests;
using Quartz;

namespace Elsa.Quartz.Jobs;

/// <summary>
/// A job that resumes a workflow.
/// </summary>
public class ResumeWorkflowJob(IWorkflowDispatcher workflowDispatcher, IJsonSerializer jsonSerializer) : IJob
{
    /// <summary>
    /// The job key.
    /// </summary>
    public static readonly JobKey JobKey = new(nameof(ResumeWorkflowJob));

    /// <inheritdoc />
    public async Task Execute(IJobExecutionContext context)
    {
        var map = context.MergedJobDataMap;
        var serializedActivityHandle = (string)map.Get(nameof(DispatchWorkflowInstanceRequest.ActivityHandle));
        var activityHandle = serializedActivityHandle != null ? jsonSerializer.Deserialize<ActivityHandle>(serializedActivityHandle) : null;
        var request = new DispatchWorkflowInstanceRequest
        {
            InstanceId = (string)map.Get(nameof(DispatchWorkflowInstanceRequest.InstanceId)),
            BookmarkId = (string)map.Get(nameof(DispatchWorkflowInstanceRequest.BookmarkId)),
            ActivityHandle = activityHandle,
            CorrelationId = (string?)map.Get(nameof(DispatchWorkflowInstanceRequest.CorrelationId)),
            Input = map.GetDictionary(nameof(DispatchWorkflowInstanceRequest.Input))
        };
        await workflowDispatcher.DispatchAsync(request, cancellationToken: context.CancellationToken);
    }
}