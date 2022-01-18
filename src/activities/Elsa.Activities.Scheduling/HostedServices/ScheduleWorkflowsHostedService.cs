using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Mediator.Contracts;
using Elsa.Persistence.Requests;
using Elsa.Scheduling.Contracts;
using Microsoft.Extensions.Hosting;
using IWorkflowTriggerScheduler = Elsa.Activities.Scheduling.Contracts.IWorkflowTriggerScheduler;

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