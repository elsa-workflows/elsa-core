using Elsa.Extensions;
using Elsa.Workflows.LogPersistence;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Middleware.Activities;
using Elsa.Workflows.State;

namespace Elsa.Workflows.Runtime;

/// <inheritdoc />
public class DefaultActivityExecutionMapper : IActivityExecutionMapper
{
    public ActivityExecutionRecord Map(ActivityExecutionContext source)
    {
        var payload = GetPayload(source);
        var outputs = source.GetOutputs();
        var inputs = source.GetInputs();
        var persistenceMap = source.TransientProperties.GetValueOrDefault(EvaluateLogPersistenceModesMiddleware.LogPersistenceMapKey, () => new ActivityLogPersistenceModeMap())!;
        var persistableInputs = GetPersistableProperties(inputs, persistenceMap.Inputs);
        var persistableOutputs = GetPersistableProperties(outputs, persistenceMap.Outputs);

        return new()
        {
            Id = source.Id,
            ActivityId = source.Activity.Id,
            ActivityNodeId = source.NodeId,
            WorkflowInstanceId = source.WorkflowExecutionContext.Id,
            ActivityType = source.Activity.Type,
            ActivityName = source.Activity.Name,
            ActivityState = persistableInputs,
            Outputs = persistableOutputs,
            Properties = source.Properties,
            Payload = payload,
            Exception = ExceptionState.FromException(source.Exception),
            ActivityTypeVersion = source.Activity.Version,
            StartedAt = source.StartedAt,
            HasBookmarks = source.Bookmarks.Any(),
            Status = source.Status,
            CompletedAt = source.CompletedAt
        };
    }

    /// <inheritdoc />
    public Task<ActivityExecutionRecord> MapAsync(ActivityExecutionContext source)
    {
        return Task.FromResult(Map(source));
    }

    private IDictionary<string, object?> GetPersistableProperties(IDictionary<string, object?> state, IDictionary<string, LogPersistenceMode> map)
    {
        var result = new Dictionary<string, object?>();
        foreach (var stateEntry in state)
        {
            var mode = map.TryGetValue(stateEntry.Key, out var value) ? value : LogPersistenceMode.Inherit;
            result.Add(stateEntry.Key, mode == LogPersistenceMode.Include ? stateEntry.Value : null);
        }

        return result;
    }
    
    private static IDictionary<string, object> GetPayload(ActivityExecutionContext source)
    {
        var outcomes = source.JournalData.TryGetValue("Outcomes", out var resultValue) ? resultValue as string[] : null;
        var payload = new Dictionary<string, object>();

        if (outcomes != null)
            payload.Add("Outcomes", outcomes);

        return payload;
    }
}