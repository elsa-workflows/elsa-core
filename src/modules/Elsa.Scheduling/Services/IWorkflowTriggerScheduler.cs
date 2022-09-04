using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Scheduling.Services;

/// <summary>
/// Schedules jobs for the specified list of workflow triggers.
/// </summary>
public interface IWorkflowTriggerScheduler
{
    Task ScheduleTriggersAsync(IEnumerable<WorkflowTrigger> triggers, CancellationToken cancellationToken = default);
    Task UnscheduleTriggersAsync(IEnumerable<WorkflowTrigger> triggers, CancellationToken cancellationToken = default);
}