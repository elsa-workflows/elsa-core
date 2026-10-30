using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;

namespace Elsa.Studio.Workflows.Helpers;

/// <summary>
/// Resolves the label to display for an activity, both on the designer and in the workflow instance views.
/// </summary>
public static class ActivityLabelResolver
{
    /// <summary>
    /// The label to use when an activity provides nothing to identify itself with.
    /// </summary>
    public const string UnknownActivityLabel = "Unknown Activity";

    /// <summary>
    /// Resolves the label using the hierarchy: custom display text > activity name > fallback.
    /// </summary>
    /// <param name="displayText">The display text the user entered for the activity.</param>
    /// <param name="name">The name of the activity, e.g. "WriteLine1".</param>
    /// <param name="fallback">The label to use when neither a display text nor a name is available, typically the activity type's display name.</param>
    /// <returns>The label to display.</returns>
    public static string Resolve(string? displayText, string? name, string? fallback = null)
    {
        if (!string.IsNullOrWhiteSpace(displayText))
            return displayText.Trim();

        if (!string.IsNullOrWhiteSpace(name))
            return name.Trim();

        return !string.IsNullOrWhiteSpace(fallback) ? fallback.Trim() : UnknownActivityLabel;
    }

    /// <summary>
    /// Resolves the label for the specified activity, falling back to the display name or name of its descriptor.
    /// </summary>
    /// <param name="activity">The activity to resolve the label for.</param>
    /// <param name="descriptor">The descriptor of the activity, if available.</param>
    /// <returns>The label to display.</returns>
    public static string Resolve(JsonObject activity, ActivityDescriptor? descriptor) =>
        Resolve(activity.GetDisplayText(), activity.GetName(), descriptor?.DisplayName ?? descriptor?.Name);
}
