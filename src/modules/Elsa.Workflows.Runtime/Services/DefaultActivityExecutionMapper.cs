using Elsa.Extensions;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.State;
using Humanizer;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Runtime;

/// <inheritdoc />
public class DefaultActivityExecutionMapper(IOptions<ManagementOptions> options) : IActivityExecutionMapper
{
    private const string LogPersistenceModeKey = "logPersistenceMode";

    /// <inheritdoc />
    public ActivityExecutionRecord Map(ActivityExecutionContext source)
    {
        /*
         * {
         *      "logPersistenceMode": {
         *          "default": "default",
         *          "inputs": { k : v },
         *          "outputs": { k: v }
         *          }
         *  }
         */

        var workflow = (Workflow?)source.GetAncestors().FirstOrDefault(x => x.Activity is Workflow)?.Activity ?? source.WorkflowExecutionContext.Workflow;
        var workflowPersistenceProperty = GetDefaultPersistenceMode(workflow.CustomProperties, () => options.Value.LogPersistenceMode);
        var activityPersistenceProperties = source.Activity.CustomProperties.GetValueOrDefault<IDictionary<string, object?>>(LogPersistenceModeKey, () => new Dictionary<string, object?>());
        var activityPersistencePropertyDefault = GetDefaultPersistenceMode(source.Activity.CustomProperties, () => workflowPersistenceProperty);

        // Get any outcomes that were added to the activity execution context.
        var outcomes = source.JournalData.TryGetValue("Outcomes", out var resultValue) ? resultValue as string[] : default;
        var payload = new Dictionary<string, object>();

        if (outcomes != null)
            payload.Add("Outcomes", outcomes);

        // Get any outputs that were added to the activity execution context.
        var activity = source.Activity;
        var expressionExecutionContext = source.ExpressionExecutionContext;
        var activityDescriptor = source.ActivityDescriptor;
        var outputDescriptors = activityDescriptor.Outputs;
        var inputDescriptors = activityDescriptor.Inputs;

        var outputs = outputDescriptors.ToDictionary(x => x.Name, x =>
        {
            if (x.IsSerializable == false)
                return "(not serializable)";

            var cachedValue = activity.GetOutput(expressionExecutionContext, x.Name);

            if (cachedValue != default)
                return cachedValue;

            if (x.ValueGetter(activity) is Output output && source.TryGet(output.MemoryBlockReference(), out var outputValue))
                return outputValue;

            return default;
        });

        outputs = StorePropertyUsingPersistenceMode(outputs, activityPersistenceProperties!.GetValueOrDefault("outputs", () => new Dictionary<string, object>())!, activityPersistencePropertyDefault);
        var inputs = StorePropertyUsingPersistenceMode(source.ActivityState, activityPersistenceProperties!.GetValueOrDefault("inputs", () => new Dictionary<string, object>())!, activityPersistencePropertyDefault);
        
        return new ActivityExecutionRecord
        {
            Id = source.Id,
            ActivityId = source.Activity.Id,
            ActivityNodeId = source.NodeId,
            WorkflowInstanceId = source.WorkflowExecutionContext.Id,
            ActivityType = source.Activity.Type,
            ActivityName = source.Activity.Name,
            ActivityState = inputs,
            Outputs = outputs,
            Properties = source.Properties,
            Payload = payload,
            Exception = ExceptionState.FromException(source.Exception),
            ActivityTypeVersion = source.Activity.Version,
            StartedAt = source.StartedAt,
            HasBookmarks = source.Bookmarks.Any(),
            Status = GetAggregateStatus(source),
            CompletedAt = source.CompletedAt
        };
    }

    private static LogPersistenceMode GetDefaultPersistenceMode(IDictionary<string, object> customProperties, Func<LogPersistenceMode> defaultFactory)
    {
        var properties = customProperties.GetValueOrDefault<IDictionary<string, object?>>(LogPersistenceModeKey, () => new Dictionary<string, object?>());
        var persistencePropertyDefault = properties!.GetValueOrDefault("default", defaultFactory);

        if (persistencePropertyDefault == LogPersistenceMode.Inherit)
            return defaultFactory();
        return persistencePropertyDefault;
    }

    private static Dictionary<string, object?> StorePropertyUsingPersistenceMode(IDictionary<string, object?> state, IDictionary<string, object> persistenceModeConfiguration, LogPersistenceMode defaultLogPersistenceMode)
    {
        var result = new Dictionary<string, object?>();

        foreach (var value in state)
        {
            var persistence = persistenceModeConfiguration.GetValueOrDefault(value.Key.Camelize(), () => defaultLogPersistenceMode);
            if (persistence.Equals(LogPersistenceMode.Include)
                || (persistence.Equals(LogPersistenceMode.Inherit) && defaultLogPersistenceMode is LogPersistenceMode.Include or LogPersistenceMode.Inherit))
                result.Add(value.Key, value.Value);
        }

        return result;
    }

    private ActivityStatus GetAggregateStatus(ActivityExecutionContext context)
    {
        // If any child activity is faulted, the aggregate status is faulted.
        var descendantContexts = context.GetDescendants().ToList();

        if (descendantContexts.Any(x => x.Status == ActivityStatus.Faulted))
            return ActivityStatus.Faulted;

        return context.Status;
    }
}