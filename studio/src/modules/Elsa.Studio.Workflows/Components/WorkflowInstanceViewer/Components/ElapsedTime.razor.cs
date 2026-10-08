using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Components.WorkflowInstanceViewer.Components;

/// <summary>
/// Represents the elapsed time between a start and end point.
/// </summary>
public partial class ElapsedTime
{
    /// <summary>
    /// Gets or sets the start time for the property.
    /// </summary>
    /// <value>The start time.</value>
    /// <remarks>
    /// The start time represents the point in time from which the property should be considered.
    /// The default value is set to the current UTC time.
    /// </remarks>
    [Parameter]
    /// <summary>
    /// Gets or sets the start time.
    /// </summary>
    public DateTimeOffset StartTime { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the end time.
    /// The end time represents the date and time when the event or activity ends.
    /// </summary>
    /// <remarks>
    /// The default value is set to the current Coordinated Universal Time (UTC) using <see cref="DateTimeOffset.UtcNow"/>.
    /// </remarks>
    /// <value>
    /// The end time as a <see cref="DateTimeOffset"/> value.
    /// </value>
    [Parameter]
    /// <summary>
    /// Gets or sets the end time.
    /// </summary>
    public DateTimeOffset EndTime { get; set; } = DateTimeOffset.UtcNow;
    
    private TimeSpan Elapsed => EndTime < StartTime ? TimeSpan.Zero : EndTime - StartTime;
}