using Elsa.Common.Contracts;
using Elsa.Extensions;
using Elsa.Scheduling;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;
using Quartz;

namespace Elsa.Quartz.Jobs;

/// <summary>
/// A job that resumes a workflow.
/// </summary>
public class ResumeWorkflowJob(IWorkflowRuntime workflowRuntime, IJsonSerializer jsonSerializer) : IJob
{
    /// <summary>
    /// The job key.
    /// </summary>
    public static readonly JobKey JobKey = new(nameof(ResumeWorkflowJob));

    /// <inheritdoc />
    public async Task Execute(IJobExecutionContext context)
    {
        var map = context.MergedJobDataMap;
        var serializedActivityHandle = (string)map.Get(nameof(ScheduleExistingWorkflowInstanceRequest.ActivityHandle));
        var activityHandle = serializedActivityHandle != null ? jsonSerializer.Deserialize<ActivityHandle>(serializedActivityHandle) : null;
        var workflowInstanceId = (string)map.Get(nameof(ScheduleExistingWorkflowInstanceRequest.WorkflowInstanceId));
        var workflowClient = await workflowRuntime.CreateClientAsync(workflowInstanceId, context.CancellationToken);
        var request = new RunWorkflowInstanceRequest
        {
            BookmarkId = (string)map.Get(nameof(ScheduleExistingWorkflowInstanceRequest.BookmarkId)),
            ActivityHandle = activityHandle,
            Input = map.GetDictionary(nameof(ScheduleExistingWorkflowInstanceRequest.Input)),
            Properties = map.GetDictionary(nameof(ScheduleExistingWorkflowInstanceRequest.Properties)),
        };
        await workflowClient.RunInstanceAsync(request, cancellationToken: context.CancellationToken);
    }
}