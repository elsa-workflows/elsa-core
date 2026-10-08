using Elsa.Api.Client.Extensions;
using Elsa.Studio.WorkflowContexts.Models;
using System.Text.Json.Nodes;

namespace Elsa.Studio.WorkflowContexts.Extensions;

/// <summary>
/// Provides extension methods for managing workflow context settings associated with activities.
/// </summary>
public static class ActivityExtensions
{
    /// <summary>
    /// Sets the workflow context settings for a given activity.
    /// </summary>
    /// <param name="activity">The activity to which the workflow context settings will be applied.</param>
    /// <param name="value">The workflow context settings to associate with the specified activity.</param>
    public static void SetWorkflowContextSettings(this JsonObject activity, Dictionary<string, ActivityWorkflowContextSettings> value)
    {
        activity.SetProperty(JsonValue.Create(value), "customProperties", "ActivityWorkflowContextSettingsKey");
    }

    /// <summary>
    /// Retrieves the workflow context settings associated with a given activity.
    /// </summary>
    /// <param name="activity">The activity from which to retrieve the workflow context settings.</param>
    /// <returns>A dictionary containing the workflow context settings associated with the specified activity.</returns>
    public static Dictionary<string, ActivityWorkflowContextSettings> GetWorkflowContextSettings(this JsonObject activity)
    {
        return activity.GetProperty<Dictionary<string, ActivityWorkflowContextSettings>>("customProperties", "ActivityWorkflowContextSettingsKey");
    }
}