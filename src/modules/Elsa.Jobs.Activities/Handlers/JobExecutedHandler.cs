using Elsa.Jobs.Activities.Helpers;
using Elsa.Jobs.Activities.Jobs;
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
        
        // When ExecuteBackgroundActivityJob finished, we know exactly what workflow to resume.
        if (job is ExecuteBackgroundActivityJob executeBackgroundActivityJob)
        {
            await _workflowRuntime.ResumeWorkflowAsync(executeBackgroundActivityJob.WorkflowInstanceId, executeBackgroundActivityJob.BookmarkId, new ResumeWorkflowOptions(), cancellationToken);
        }
        else
        {
            // When any other type of job finished, we resume any workflow blocked on this job. 
            var jobType = notification.Job.GetType();
            var payload = new EnqueuedJobPayload(job.Id);
            var jobTypeName = JobTypeNameHelper.GenerateTypeName(jobType);
            await _workflowRuntime.ResumeWorkflowsAsync(jobTypeName, payload, new ResumeWorkflowOptions(), cancellationToken);    
        }
    }
}