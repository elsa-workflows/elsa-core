using Elsa.Jobs.Activities.Helpers;
using Elsa.Jobs.Activities.Models;
using Elsa.Jobs.Notifications;
using Elsa.Mediator.Services;

namespace Elsa.Jobs.Activities.Handlers;

public class JobExecutedHandler : INotificationHandler<JobExecuted>
{
    // private readonly IWorkflowService _workflowService;
    //
    // public JobExecutedHandler(IWorkflowService workflowService)
    // {
    //     _workflowService = workflowService;
    // }
    
    public async Task HandleAsync(JobExecuted notification, CancellationToken cancellationToken)
    {
        var payload = new EnqueuedJobPayload(notification.Job.Id);
        var jobType = notification.Job.GetType();
        var jobTypeName = JobTypeNameHelper.GenerateTypeName(jobType);
        var bookmarkName = jobTypeName;
        //await _workflowService.DispatchStimulusAsync(bookmarkName, payload, cancellationToken: cancellationToken);
    }
}