using Elsa.Workflows.Core.Contracts;

namespace Elsa.Workflows.Core.Signals;

/// <summary>
/// Signaled when the scheduling of a child activity was requested.
/// </summary>
/// <param name="Activity">The child activity to schedule.</param>
public record ScheduleChildActivity(IActivity Activity);