using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.WorkflowContexts.Models;
using Elsa.Workflows;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Extension methods for <see cref="IActivity"/>.
/// </summary>
public static class ActivityExtensions
{
    private const string ActivityWorkflowContextSettingsKey = "ActivityWorkflowContextSettingsKey";
    
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase, 
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Gets the workflow context settings for the specified activity.
    /// </summary>
    /// <param name="activity">The activity to get the settings for.</param>
    /// <returns>The workflow context settings.</returns>
    public static IDictionary<Type, ActivityWorkflowContextSettings> GetWorkflowContextSettings(this IActivity activity)
    {
        var contextSettings =  activity.CustomProperties.GetOrAdd(ActivityWorkflowContextSettingsKey, () => new JsonObject());
        var result = new Dictionary<Type, ActivityWorkflowContextSettings>();

        foreach(var (key, jsonNode) in contextSettings)
        {
            var targetType = Type.GetType(key);

            if(targetType != null)
            {
                var value = jsonNode.Deserialize<ActivityWorkflowContextSettings>(JsonSerializerOptions)!;
                result.Add(targetType, value);
            }
        }

        return result;
    }

    /// <summary>
    /// Configures the activity to load the workflow context before executing.
    /// </summary>
    /// <param name="activity">The activity to configure.</param>
    /// <param name="providerType">The type of the workflow context provider.</param>
    /// <typeparam name="TActivity">The type of the activity.</typeparam>
    /// <returns>The activity.</returns>
    public static TActivity LoadContext<TActivity>(this TActivity activity, Type providerType) where TActivity : IActivity
    {
        var settings = activity.GetActivityWorkflowContextSettings(providerType);
        settings.Load = true;
        return activity;
    }
    
    /// <summary>
    /// Configures the activity to save the workflow context after executing.
    /// </summary>
    /// <param name="activity">The activity to configure.</param>
    /// <param name="providerType">The type of the workflow context provider.</param>
    /// <typeparam name="TActivity">The type of the activity.</typeparam>
    /// <returns>The activity.</returns>
    public static TActivity SaveContext<TActivity>(this TActivity activity, Type providerType) where TActivity : IActivity
    {
        var settings = activity.GetActivityWorkflowContextSettings(providerType);
        settings.Save = true;
        return activity;
    }

    /// <summary>
    /// Gets the workflow context settings for the specified activity.
    /// </summary>
    /// <param name="activity">The activity to get the settings for.</param>
    /// <param name="providerType">The type of the workflow context provider.</param>
    /// <typeparam name="TActivity">The type of the activity.</typeparam>
    /// <returns>The workflow context settings.</returns>
    public static ActivityWorkflowContextSettings GetActivityWorkflowContextSettings<TActivity>(this TActivity activity, Type providerType) where TActivity: IActivity
    {
        var dictionary = activity.GetWorkflowContextSettings();
        return dictionary.GetActivityWorkflowContextSettings(providerType);
    }

    /// <summary>
    /// Gets the workflow context settings for the specified activity.
    /// </summary>
    /// <param name="dictionary">The dictionary to get the settings from.</param>
    /// <param name="providerType">The type of the workflow context provider.</param>
    /// <returns>The workflow context settings.</returns>
    public static ActivityWorkflowContextSettings GetActivityWorkflowContextSettings(this IDictionary<Type, ActivityWorkflowContextSettings> dictionary, Type providerType)
    {
        var settings = dictionary.TryGetValue(providerType, out ActivityWorkflowContextSettings? value) ? value : default;
        
        if(settings == null)
        {
            settings = new ActivityWorkflowContextSettings();
            dictionary[providerType] = settings;
        }

        return settings;
    }
}