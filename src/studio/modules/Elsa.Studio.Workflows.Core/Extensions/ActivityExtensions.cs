using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;

namespace Elsa.Studio.Workflows.Extensions;

/// <summary>
/// Provides extension methods for <see cref="JsonObject"/> representing an activity
/// </summary>
public static class ActivityExtensions
{
    /// <summary>
    /// Gets the flowchart from the specified activity.
    /// </summary>
    /// <param name="activity"></param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="NotSupportedException"></exception>
    public static JsonObject? GetFlowchart(this JsonObject activity)
    {
        var activityTypeName = activity.GetTypeName();

        if (activityTypeName == "Elsa.Flowchart")
            return activity;

        if (activityTypeName == "Elsa.Workflow")
            return activity.GetRoot()!;

        return null;
    }

    /// <summary>
    /// Recursively looks for the first JsonObject that has an "activities" array.
    /// If none is found, returns null.
    /// </summary>
    public static JsonObject? FindActivitiesContainer(this JsonObject activity)
    {
        // 0) If *this* object is already a container, return it.
        // TODO: Think of a more extensible way to do this. Perhaps adding an attribute called "container" to the activity type?
        if (activity.GetTypeName() == "Elsa.Flowchart")
            return activity;
        
        // 1) If *this* object has an "activities" array, it is the container.
        if (activity.TryGetPropertyValue("activities", out var maybeArr) && maybeArr is JsonArray)
            return activity;

        // 2) Otherwise, scan every child property.
        foreach (var kvp in activity)
        {
            // if it's a JsonObject, recurse into it.
            if (kvp.Value is JsonObject childObj)
            {
                var found = childObj.FindActivitiesContainer();
                if (found != null)
                    return found;
            }

            // if it's a JsonArray, recurse into each item.
            if (kvp.Value is JsonArray childArr)
            {
                foreach (var node in childArr.OfType<JsonObject>())
                {
                    var found = node.FindActivitiesContainer();
                    if (found != null)
                        return found;
                }
            }
        }

        // 3) No container found.
        return null;
    }
}
