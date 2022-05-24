using Elsa.Services;
using Elsa.WorkflowContexts.Models;

namespace Elsa.WorkflowContexts.Extensions;

public static class ActivityExtensions
{
    private const string ActivityWorkflowContextSettingsKey = "ActivityWorkflowContextSettingsKey";

    public static IDictionary<WorkflowContext, ActivityWorkflowContextSettings> GetWorkflowContextSettings(this IActivity activity)
    {
        return activity.ApplicationProperties.GetOrAdd(ActivityWorkflowContextSettingsKey, () => new Dictionary<WorkflowContext, ActivityWorkflowContextSettings>())!;
    }

    public static TActivity LoadContext<TActivity>(this TActivity activity, WorkflowContext workflowContext) where TActivity : IActivity
    {
        var settings = activity.GetActivityWorkflowContextSettings(workflowContext);
        settings.Load = true;
        return activity;
    }
    
    public static TActivity SaveContext<TActivity>(this TActivity activity, WorkflowContext workflowContext) where TActivity : IActivity
    {
        var settings = activity.GetActivityWorkflowContextSettings(workflowContext);
        settings.Save = true;
        return activity;
    }

    public static ActivityWorkflowContextSettings GetActivityWorkflowContextSettings<TActivity>(this TActivity activity, WorkflowContext workflowContext) where TActivity: IActivity
    {
        var dictionary = activity.GetWorkflowContextSettings()!;
        return dictionary.GetActivityWorkflowContextSettings(workflowContext);
    }

    public static ActivityWorkflowContextSettings GetActivityWorkflowContextSettings(this IDictionary<WorkflowContext, ActivityWorkflowContextSettings> dictionary, WorkflowContext workflowContext)
    {
        var settings = dictionary.ContainsKey(workflowContext) ? dictionary[workflowContext] : default;
        
        if(settings == null)
        {
            settings = new ActivityWorkflowContextSettings();
            dictionary[workflowContext] = settings;
        }

        return settings;
    }
}