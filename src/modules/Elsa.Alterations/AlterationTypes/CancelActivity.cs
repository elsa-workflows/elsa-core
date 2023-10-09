using Elsa.Alterations.Core.Abstractions;

namespace Elsa.Alterations.AlterationTypes;

/// <summary>
/// Cancels an activity.
/// </summary>
public class CancelActivity : AlterationBase
{
    /// <summary>
    /// The ID of the activity to be cancelled. If not specified, the activity instance ID will be used.
    /// </summary>
    public string? ActivityId { get; set; }

    /// <summary>
    /// The ID of the activity instance to be cancelled. If specified, overrides <see cref="ActivityId"/>.
    /// </summary>
    public string? ActivityInstanceId { get; set; }
}