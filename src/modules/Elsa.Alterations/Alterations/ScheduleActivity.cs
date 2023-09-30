using Elsa.Alterations.Core.Abstractions;

namespace Elsa.Alterations;

/// <summary>
/// Schedules a flowchart activity.
/// </summary>
public class ScheduleActivity : AlterationBase
{
    /// <summary>
    /// The Id of the next activity to be scheduled.
    /// </summary>
    public string ActivityId { get; set; } = default!;
}