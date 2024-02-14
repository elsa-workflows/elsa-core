using Elsa.Extensions;
using Elsa.Workflows.Enums;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.State;

namespace Elsa.Workflows.Runtime.Services;

/// <inheritdoc />
public class DefaultActivityExecutionMapper : IActivityExecutionMapper
{
    private PersistenceStrategy _serverPersistenceStrategyProvider;

    public DefaultActivityExecutionMapper(IWorkflowActivityPersistenceStrategyProvider serverPersistenceStrategyProvider)
    {
        _serverPersistenceStrategyProvider =  serverPersistenceStrategyProvider.GetGlobalPersistenceStrategy();
    }

    /// <inheritdoc />
    public ActivityExecutionRecord Map(ActivityExecutionContext source)
    {
        /*
        * { 
        *      "persistence": {
        *          "default": "default",
        *          "inputs": { k : v }, 
        *          "outputs": { k: v }
        *          }
        *  }
        */

        var workflowPersistenceProperty = source.WorkflowExecutionContext.Workflow.CustomProperties
                            .GetValueOrDefault<PersistenceStrategy>("persistence", () => _serverPersistenceStrategyProvider);
       
        var activityPersistenceProperties = source.Activity.CustomProperties
            .GetValueOrDefault<IDictionary<string, object?>>("persistence", () => new Dictionary<string, object?>());
        var activityPersistencePropertyDefault = activityPersistenceProperties!
            .GetValueOrDefault("default",()=> workflowPersistenceProperty);
 
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

        outputs = StorePropertyUsingStrategy(outputs, activityPersistenceProperties.GetValueOrDefault("outputs", () => new Dictionary<string, object>()), activityPersistencePropertyDefault);
        var activityState = StorePropertyUsingStrategy(source.ActivityState, activityPersistenceProperties.GetValueOrDefault("inputs", () => new Dictionary<string, object>()), activityPersistencePropertyDefault );

        return new ActivityExecutionRecord
        {
            Id = source.Id,
            ActivityId = source.Activity.Id,
            ActivityNodeId = source.NodeId,
            WorkflowInstanceId = source.WorkflowExecutionContext.Id,
            ActivityType = source.Activity.Type,
            ActivityName = source.Activity.Name,
            ActivityState = activityState,
            Outputs = outputs,
            Payload = payload,
            Exception = ExceptionState.FromException(source.Exception),
            ActivityTypeVersion = source.Activity.Version,
            StartedAt = source.StartedAt,
            HasBookmarks = source.Bookmarks.Any(),
            Status = GetAggregateStatus(source),
            CompletedAt = source.CompletedAt
        };
    }

    private static Dictionary<string,object?> StorePropertyUsingStrategy(IDictionary<string,object?> inputs
        , IDictionary<string,object> strategies
        , PersistenceStrategy defaultPersistenceStrategy = PersistenceStrategy.Exclude)
    {
        var result = new Dictionary<string, object?>();

        foreach (var input in inputs)
        {
            var persistence = strategies.GetValueOrDefault(input.Key, () => defaultPersistenceStrategy);
            if (persistence.Equals(PersistenceStrategy.Include))
                result.Add(input.Key, input.Value);
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