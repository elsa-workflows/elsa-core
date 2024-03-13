using Elsa.Workflows;
using JetBrains.Annotations;

namespace Elsa.Alterations.Core.Models;

/// <summary>
/// Represents a filter for activities.
/// </summary>
[UsedImplicitly]
public class ActivityFilter
{
    /// <summary>
    /// The ID of the activity.
    /// </summary>
    public string? Id { get; set; }

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