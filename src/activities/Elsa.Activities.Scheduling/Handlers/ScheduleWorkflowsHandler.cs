using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Scheduling.Contracts;
using Elsa.Activities.Scheduling.Jobs;
using Elsa.Activities.Scheduling.Schedules;
using Elsa.Mediator.Contracts;
using Elsa.Persistence.Requests;
using Elsa.Runtime.Notifications;

namespace Elsa.Activities.Scheduling.Handlers;

public class ScheduleWorkflowsHandler : INotificationHandler<TriggerIndexingFinished>
{
    private readonly IRequestSender _requestSender;
    private readonly IJobScheduler _jobScheduler;

    public ScheduleWorkflowsHandler(IRequestSender requestSender, IJobScheduler jobScheduler)
    {
        _requestSender = requestSender;
        _jobScheduler = jobScheduler;
    }

    public async Task HandleAsync(TriggerIndexingFinished notification, CancellationToken cancellationToken)
    {
        var timerTriggers = await _requestSender.RequestAsync(FindWorkflowTriggers.ForTrigger<Timer>(), cancellationToken);

        foreach (var trigger in timerTriggers)
        {
            var (dateTime, timeSpan) = JsonSerializer.Deserialize<TimerPayload>(trigger.Payload!)!;
            await _jobScheduler.ScheduleAsync(new RunWorkflowJob(trigger.WorkflowDefinitionId), new RecurringSchedule(dateTime, timeSpan), cancellationToken);
        }
    }
}