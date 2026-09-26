using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;

namespace Elsa.Studio.Workflows.Extensions;

/// <summary>
/// Provides extension methods for locating and replacing activities within a <see cref="JsonObject"/> activity tree.
/// </summary>
public static class JsonObjectActivityTreeExtensions
{
    /// <summary>
    /// Recursively searches the specified scope's <c>activities</c> array for an activity with the given ID and,
    /// when found, replaces it with a deep clone of <paramref name="replacement"/>.
    /// </summary>
    /// <param name="scope">The activity whose <c>activities</c> array is searched.</param>
    /// <param name="id">The ID of the activity to replace.</param>
    /// <param name="replacement">The activity to replace the matched activity with.</param>
    /// <returns><c>true</c> if an activity with the specified ID was found and replaced; otherwise, <c>false</c>.</returns>
    public static bool ReplaceActivity(this JsonObject scope, string id, JsonObject replacement)
    {
        if (scope["activities"] is not JsonArray activities)
            return false;

        for (var i = 0; i < activities.Count; i++)
        {
            if (activities[i] is not JsonObject child)
                continue;

            if (child.GetId() == id)
            {
                activities[i] = (JsonObject)replacement.DeepClone()!;
                return true;
            }

            if (child.ReplaceActivity(id, replacement))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Finds the activity with the given ID: <paramref name="scope"/> itself, or, recursively, an activity in its
    /// <c>activities</c> array — the shape a BPMN process scope and every scope nested in it share.
    /// </summary>
    /// <returns>The activity, or <c>null</c> when neither the scope nor any activity under it has that ID.</returns>
    public static JsonObject? FindActivity(this JsonObject scope, string id) =>
        scope.GetId() == id
            ? scope
            : (scope["activities"] as JsonArray ?? []).OfType<JsonObject>().Select(child => child.FindActivity(id)).FirstOrDefault(found => found != null);
}
