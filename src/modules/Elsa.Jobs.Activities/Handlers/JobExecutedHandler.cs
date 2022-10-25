using Elsa.Jobs.Activities.Helpers;
using Elsa.Jobs.Activities.Models;
using Elsa.Jobs.Notifications;
using Elsa.Mediator.Services;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.Jobs.Activities.Handlers;

public class JobExecutedHandler : INotificationHandler<JobExecuted>
{
    private readonly IWorkflowRuntime _workflowRuntime;

    public JobExecutedHandler(IWorkflowRuntime workflowRuntime)
    {
        _workflowRuntime = workflowRuntime;
    }
    
    public async Task HandleAsync(JobExecuted notification, CancellationToken cancellationToken)
    {
        var job = notification.Job;
        var payload = new EnqueuedJobPayload(job.Id);
        var jobType = notification.Job.GetType();
        var jobTypeName = JobTypeNameHelper.GenerateTypeName(jobType);
        await _workflowRuntime.ResumeWorkflowsAsync(jobTypeName, payload, new ResumeWorkflowOptions(), cancellationToken);
    }
}