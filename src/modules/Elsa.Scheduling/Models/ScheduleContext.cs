namespace Elsa.Scheduling;

/// <summary>
/// The context for scheduling a task.
/// </summary>
/// <param name="ServiceProvider">The service provider.</param>
/// <param name="Task">The task to schedule.</param>
public record ScheduleContext(IServiceProvider ServiceProvider, ITask Task);