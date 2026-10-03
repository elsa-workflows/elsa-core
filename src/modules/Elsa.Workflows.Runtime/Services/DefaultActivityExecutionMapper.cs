using Elsa.Common;
using Elsa.Common.Codecs;
using Elsa.Workflows.LogPersistence;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Extensions;
using Elsa.Workflows.State;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Runtime;

/// <inheritdoc />
public class DefaultActivityExecutionMapper(
    ISafeSerializer safeSerializer,
    IPayloadSerializer payloadSerializer,
    ICompressionCodecResolver compressionCodecResolver,
    IOptions<ManagementOptions> options) : IActivityExecutionMapper
{
    /// <inheritdoc />
    public async Task<ActivityExecutionRecord> MapAsync(ActivityExecutionContext source)
    {
        var outputs = source.GetOutputs();
        var inputs = source.GetInputs();
        var persistenceMap = source.GetLogPersistenceModeMap();
        var persistableInputs = GetPersistableInputOutput(inputs, persistenceMap.Inputs);
        var persistableOutputs = GetPersistableInputOutput(outputs, persistenceMap.Outputs);
        var persistableProperties = GetPersistableDictionary(source.Properties!, persistenceMap.InternalState);
        var persistableJournalData = GetPersistableDictionary(source.JournalData!, persistenceMap.InternalState);
        var cancellationToken = source.CancellationToken;

        var record = new ActivityExecutionRecord
        {
            Id = source.Id,
            TenantId = source.WorkflowExecutionContext.Workflow?.Identity?.TenantId,
            ActivityId = source.Activity.Id,
            ActivityNodeId = source.NodeId,
            WorkflowInstanceId = source.WorkflowExecutionContext.Id,
            ActivityType = source.Activity.Type,
            ActivityName = source.Activity.Name,
            ActivityState = persistableInputs,
            Outputs = persistableOutputs,
            Properties = persistableProperties!,
            Metadata = new Dictionary<string, object>(source.Metadata),
            Payload = persistableJournalData!,
            Exception = ExceptionState.FromException(source.Exception),
            ActivityTypeVersion = source.Activity.Version,
            StartedAt = source.StartedAt,
            HasBookmarks = source.Bookmarks.Any(),
            Status = source.Status,
            AggregateFaultCount = source.AggregateFaultCount,
            CompletedAt = source.CompletedAt,
            SchedulingActivityExecutionId = source.SchedulingActivityExecutionId,
            SchedulingActivityId = source.SchedulingActivityId,
            SchedulingWorkflowInstanceId = source.SchedulingWorkflowInstanceId,
            CallStackDepth = source.CallStackDepth
        };

        record = record.SanitizeLogMessage();
        var compressionAlgorithm = options.Value.CompressionAlgorithm ?? nameof(None);
        var serializedActivityState = record.ActivityState?.Count > 0 ? safeSerializer.Serialize(record.ActivityState) : null;
        var compressedSerializedActivityState = serializedActivityState != null ? await compressionCodecResolver.Resolve(compressionAlgorithm).CompressAsync(serializedActivityState, cancellationToken) : null;
        var serializedProperties = record.Properties != null ? payloadSerializer.Serialize(record.Properties) : null;
        var serializedMetadata = record.Metadata != null ? payloadSerializer.Serialize(record.Metadata) : null;
        record.SerializedSnapshot = new()
        {
            Id = record.Id,
            TenantId = record.TenantId,
            WorkflowInstanceId = record.WorkflowInstanceId,
            ActivityId = record.ActivityId,
            ActivityNodeId = record.ActivityNodeId,
            ActivityType = record.ActivityType,
            ActivityTypeVersion = record.ActivityTypeVersion,
            ActivityName = record.ActivityName,
            StartedAt = record.StartedAt,
            HasBookmarks = record.HasBookmarks,
            Status = record.Status,
            AggregateFaultCount = record.AggregateFaultCount,
            CompletedAt = record.CompletedAt,
            SerializedActivityState = compressedSerializedActivityState,
            SerializedActivityStateCompressionAlgorithm = compressionAlgorithm,
            SerializedOutputs = record.Outputs?.Any() == true ? safeSerializer.Serialize(record.Outputs) : null,
            SerializedProperties = serializedProperties,
            SerializedMetadata = serializedMetadata,
            SerializedException = record.Exception != null ? payloadSerializer.Serialize(record.Exception) : null,
            SerializedPayload = record.Payload?.Any() == true ? payloadSerializer.Serialize(record.Payload) : null
        };
        
        return record;
    }

    private IDictionary<string, object?> GetPersistableInputOutput(IDictionary<string, object> state, IDictionary<string, LogPersistenceMode> map, bool deepCopy = false)
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
        return mode == LogPersistenceMode.Include ? new Dictionary<string, object?>(dictionary) : null;
    }
}