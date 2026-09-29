using System.Text.Json.Nodes;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Extensions;

namespace Elsa.Studio.Workflows.Domain.Models;

/// An activity and a lookup of its descendants.
public class ActivityGraph(JsonObject activity, IActivityVisitor activityVisitor)
{
    private IDictionary<string, ActivityNode> _activityNodeLookup = new Dictionary<string, ActivityNode>();

    /// <summary>
    /// The root activity.
    /// </summary>
    public JsonObject Activity { get; } = activity;

    /// <summary>
    /// A lookup of activity nodes by ID.
    /// </summary>
    public IReadOnlyDictionary<string, ActivityNode> ActivityNodeLookup => _activityNodeLookup.AsReadOnly();

    /// <summary>
    /// Updates the activity node lookup.
    /// </summary>
    public async Task<ActivityGraph> IndexAsync()
    {
        _activityNodeLookup = await activityVisitor.VisitAndMapAsync(Activity);
        return this;
    }
}