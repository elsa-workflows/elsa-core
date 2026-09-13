using Quartz;

namespace Elsa.Scheduling.Quartz.Jobs;

/// <summary>
/// Durable, triggerless Quartz job used as a cancellation and schedule-generation marker for an original schedule.
/// Marker rows are intentionally retained and updated rather than deleted during rescheduling.
/// </summary>
internal sealed class QuartzScheduleCancellationJob : IJob
{
    /// <inheritdoc />
    public Task Execute(IJobExecutionContext context) => Task.CompletedTask;
}
