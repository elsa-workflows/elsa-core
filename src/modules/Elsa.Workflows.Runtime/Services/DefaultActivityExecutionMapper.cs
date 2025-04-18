using Elsa.Workflows.LogPersistence;
using Elsa.Workflows.Runtime.Entities;
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
        var persistenceMap = source.GetLogPersistenceModeMap();
        var persistableInputs = GetPersistableInputOutput(inputs, persistenceMap.Inputs);
        var persistableOutputs = GetPersistableInputOutput(outputs, persistenceMap.Outputs);
        var persistableProperties = GetPersistableDictionary(source.Properties!, persistenceMap.InternalState);
        var persistablePayload = GetPersistableDictionary(payload!, persistenceMap.InternalState);

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
            Properties = persistableProperties,
            Payload = persistablePayload!,
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

    private IDictionary<string, object?> GetPersistableInputOutput(IDictionary<string, object> state, IDictionary<string, LogPersistenceMode> map)
    {
        var result = new Dictionary<string, object?>();
        foreach (var stateEntry in state)
        {
            var mode = map.TryGetValue(stateEntry.Key, out var value) ? value : LogPersistenceMode.Include;
            if (mode == LogPersistenceMode.Include)
                result.Add(stateEntry.Key, stateEntry.Value);
        }

        return result;
    }

    private IDictionary<string, object?>? GetPersistableDictionary(IDictionary<string, object?> dictionary, LogPersistenceMode mode)
    {
        return mode == LogPersistenceMode.Include ? dictionary : null;
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