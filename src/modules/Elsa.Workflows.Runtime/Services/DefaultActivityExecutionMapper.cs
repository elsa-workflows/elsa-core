using Elsa.Workflows.LogPersistence;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.State;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime;

/// <inheritdoc />
public class DefaultActivityExecutionMapper(ILogger<DefaultActivityExecutionMapper> logger) : IActivityExecutionMapper
{
    public ActivityExecutionRecord Map(ActivityExecutionContext source)
    {
        var outputs = source.GetOutputs();
        var inputs = source.GetInputs();
        var persistenceMap = source.GetLogPersistenceModeMap();
        var persistableInputs = GetPersistableInputOutput(inputs, persistenceMap.Inputs);
        var persistableOutputs = GetPersistableInputOutput(outputs, persistenceMap.Outputs);
        var persistableProperties = GetPersistableDictionary(source.Properties!, persistenceMap.InternalState);
        var persistableJournalData = GetPersistableDictionary(source.JournalData!, persistenceMap.InternalState);

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
            Payload = persistableJournalData!,
            Exception = ExceptionState.FromException(source.Exception),
            ActivityTypeVersion = source.Activity.Version,
            StartedAt = source.StartedAt,
            HasBookmarks = source.Bookmarks.Any(),
            Status = source.Status,
            AggregateFaultCount = source.AggregateFaultCount,
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
        logger.LogDebug("Log Persistence Mode for internal state: {LogPersistenceMode}", mode);
        return mode == LogPersistenceMode.Include ? new Dictionary<string, object?>(dictionary) : null;
    }
}