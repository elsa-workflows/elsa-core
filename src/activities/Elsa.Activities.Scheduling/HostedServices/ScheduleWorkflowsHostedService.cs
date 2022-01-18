using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Scheduling.Contracts;
using Elsa.Mediator.Contracts;
using Elsa.Persistence.Requests;
using Microsoft.Extensions.Hosting;

namespace Elsa.Activities.Scheduling.HostedServices;

/// <summary>
/// Loads all timer-specific workflow triggers from the database and create scheduled jobs for them. 
/// </summary>
public class ScheduleWorkflowsHostedService : BackgroundService
{
    private readonly IRequestSender _requestSender;
    private readonly IWorkflowTriggerScheduler _workflowTriggerScheduler;

    public ScheduleWorkflowsHostedService(IRequestSender requestSender, IWorkflowTriggerScheduler workflowTriggerScheduler)
    {
        _requestSender = requestSender;
        _workflowTriggerScheduler = workflowTriggerScheduler;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timerTriggers = (await _requestSender.RequestAsync(FindWorkflowTriggers.ForTrigger<Timer>(), stoppingToken)).ToImmutableList();
        await _workflowTriggerScheduler.ScheduleTriggersAsync(timerTriggers, stoppingToken);
    }
}