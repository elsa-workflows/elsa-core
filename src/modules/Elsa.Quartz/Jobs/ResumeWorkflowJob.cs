using Elsa.Extensions;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Models.Requests;
using Quartz;

namespace Elsa.Quartz.Jobs;

/// <summary>
/// A job that resumes a workflow.
/// </summary>
public class ResumeWorkflowJob : IJob
{
    private readonly IWorkflowDispatcher _workflowDispatcher;

    /// <summary>
    /// The job key.
    /// </summary>
    public static readonly JobKey JobKey = new(nameof(ResumeWorkflowJob));

    /// <summary>
    /// Initializes a new instance of the <see cref="ResumeWorkflowJob"/> class.
    /// </summary>
    public ResumeWorkflowJob(IWorkflowDispatcher workflowDispatcher)
    {
        _workflowDispatcher = workflowDispatcher;
    }

    /// <inheritdoc />
    public async Task Execute(IJobExecutionContext context)
    {
        var map = context.MergedJobDataMap;
        var request = new DispatchWorkflowInstanceRequest
        {
            InstanceId = (string)map.Get(nameof(DispatchWorkflowInstanceRequest.InstanceId)),
            BookmarkId = (string)map.Get(nameof(DispatchWorkflowInstanceRequest.BookmarkId)),
            ActivityId = (string?)map.Get(nameof(DispatchWorkflowInstanceRequest.ActivityId)),
            ActivityNodeId = (string?)map.Get(nameof(DispatchWorkflowInstanceRequest.ActivityNodeId)),
            ActivityInstanceId = (string?)map.Get(nameof(DispatchWorkflowInstanceRequest.ActivityInstanceId)),
            CorrelationId = (string?)map.Get(nameof(DispatchWorkflowInstanceRequest.CorrelationId)),
            Input = map.GetDictionary(nameof(DispatchWorkflowInstanceRequest.Input))
        };
        await _workflowDispatcher.DispatchAsync(request, context.CancellationToken);
    }
}