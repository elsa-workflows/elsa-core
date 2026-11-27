using Elsa.Workflows.Management.Entities;

namespace Elsa.Workflows.Runtime.Options;

/// <summary>
/// Provides options to the workflow runtime.
/// </summary>
public class RuntimeOptions
{
    /// <summary>
    /// A list of workflow builders configured during application startup.
    /// </summary>
    public IDictionary<string, Func<IServiceProvider, ValueTask<IWorkflow>>> Workflows { get; set; } = new Dictionary<string, Func<IServiceProvider, ValueTask<IWorkflow>>>();

    /// <summary>
    /// The default workflow liveness threshold.
    /// </summary>
    /// <remarks>
    /// The liveness threshold is used to determine if a persisted workflow instance in the <see cref="WorkflowSubStatus.Executing"/> state should be considered interrupted or not.
    /// Interrupted workflows will be attempted to be restarted.
    /// A separate heartbeat process will ensure the <see cref="WorkflowInstance.UpdatedAt"/> is updated before this threshold.
    /// If the workflow instance got removed from memory, e.g. because of an application shutdown, the LastUpdated field will eventually exceed the liveness threshold and therefore be considered to be interrupted.
    /// </remarks>
    public TimeSpan InactivityThreshold { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// The number of workflow instances to restart in a single batch.
    /// </summary>
    /// <remarks>
    /// The batch size represents the number of workflow instance records to load into memory at a time.
    /// This provides control over memory consumption of the application.
    /// </remarks>
    public int RestartInterruptedWorkflowsBatchSize { get; set; } = 100;

    /// <summary>
    /// The names of the scheduled triggers: the triggers that a trigger scheduler schedules by their trigger ID, such as timers and cron.
    /// Every other trigger is unscheduled.
    /// </summary>
    /// <remarks>
    /// The module that schedules a trigger type adds its name, and the trigger schedulers select the triggers they schedule from this set.
    /// A host or module with its own trigger scheduler must add the names of the triggers it schedules, or those triggers are never loaded
    /// for scheduling. A tenant-agnostic workflow definition stores each unscheduled trigger once for all tenants and each scheduled trigger
    /// once per tenant.
    /// </remarks>
    public ISet<string> ScheduledTriggerNames { get; set; } = new HashSet<string>(StringComparer.Ordinal);
}