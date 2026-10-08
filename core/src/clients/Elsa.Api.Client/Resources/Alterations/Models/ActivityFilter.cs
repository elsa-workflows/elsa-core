using Elsa.Api.Client.Resources.Alterations.Enums;

namespace Elsa.Api.Client.Resources.Alterations.Models;

/// <summary>
/// A filter for activities within a workflow instance
/// </summary>
public class ActivityFilter
{
    /// <summary>
    /// The ID of the activity.
    /// </summary>
    public string? ActivityId { get; set; }
    
    /// <summary>
    /// The ID of the activity instance.
    /// </summary>
    public string? ActivityInstanceId { get; set; }

    /// <summary>
    /// The node ID of the activity.
    /// </summary>
    public string? NodeId { get; set; }

    /// <summary>
    /// The name of the activity.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The status of the activity.
    /// </summary>
    public ActivityStatus? Status { get; set; }
}