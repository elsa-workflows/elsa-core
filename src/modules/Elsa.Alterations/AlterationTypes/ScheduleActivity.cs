using Elsa.Alterations.Core.Abstractions;

namespace Elsa.Alterations.AlterationTypes;

/// <summary>
/// Schedules an activity for execution.
/// </summary>
public class ScheduleActivity : AlterationBase
{
    /// <summary>
    /// The ID of the next activity to be scheduled. If not specified, the ActivityInstanceId will be used.
    /// </summary>
    public string? ActivityId { get; set; }

    /// <summary>
    /// The ID of the activity instance to be scheduled. If not specified, the ActivityId will be used.
    /// </summary>
    public string? ActivityInstanceId { get; set; }
}