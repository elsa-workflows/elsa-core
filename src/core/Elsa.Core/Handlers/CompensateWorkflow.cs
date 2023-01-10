using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Compensation;
using Elsa.Events;
using Elsa.Services.Compensation;
using MediatR;

namespace Elsa.Handlers;

/// <summary>
/// Handles the <see cref="WorkflowFaulting"/> event by trying to compensate the workflow in case it contains <see cref="Compensable"/> activities.
/// </summary>
public class CompensateWorkflow : INotificationHandler<WorkflowFaulting>
{
    private readonly ICompensationService _compensationService;

    public CompensateWorkflow(ICompensationService compensationService)
    {
        _compensationService = compensationService;
    }

    public Task Handle(WorkflowFaulting notification, CancellationToken cancellationToken)
    {
        var activityExecutionContext = notification.ActivityExecutionContext;
        var exception = activityExecutionContext.WorkflowExecutionContext.Exception;
        var message = activityExecutionContext.WorkflowInstance.Faults.FirstOrDefault(x => x.FaultedActivityId == notification.ActivityExecutionContext.ActivityId)?.Message;
        _compensationService.Compensate(notification.ActivityExecutionContext, exception, message);
        return Task.CompletedTask;
    }
}