using Elsa.Studio.Workflows.UI.Models;

namespace Elsa.Studio.Workflows.UI.Contracts;

/// <summary>
/// Provides mappings between activity types and icons.
/// </summary>
public interface IActivityDisplaySettingsRegistry
{
    /// <summary>
    /// Retrieves the display settings for the specified activity type.
    /// </summary>
    /// <param name="activityType">The activity type.</param>
    /// <returns>The display settings for the specified activity type.</returns>
    ActivityDisplaySettings GetSettings(string activityType);

    /// <summary>
    /// Marks the activity display settings registry as stale.
    /// </summary>
    void MarkStale();
}