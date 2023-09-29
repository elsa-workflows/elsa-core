using Elsa.Alterations.Core.Abstractions;

namespace Elsa.Alterations.AlterationTypes;

public class ScheduleFlowchartActivity : AlterationBase
{
    /// <summary>
    /// The Id of the container activity.
    /// </summary>
    public string FlowchartId { get; set; } = default!;

    /// <summary>
    /// The Id of the next activity to be scheduled.
    /// </summary>
    public string NextActivityId { get; set; } = default!;
}