using Elsa.Alterations.Core.Abstractions;

namespace Elsa.Alterations.AlterationTypes;

/// <summary>
/// Schedules an activity for execution.
/// </summary>
public class ScheduleActivity : AlterationBase
{
    /// <summary>
    /// The ID of the next activity to be scheduled.
    /// </summary>
    public string ActivityId { get; set; } = default!;
}