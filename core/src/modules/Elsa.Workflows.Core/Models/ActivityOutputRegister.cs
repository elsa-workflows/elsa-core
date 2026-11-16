namespace Elsa.Workflows.Models;

/// <summary>
/// Stores activity output.
/// </summary>
public class ActivityOutputRegister
{
    private readonly Dictionary<string, List<ActivityOutputRecord>> _recordsByActivityIdAndOutputName = new();
    private readonly Dictionary<string, ActivityOutputRecord> _recordsByActivityInstanceIdAndOutputName = new();

    /// <summary>
    /// The default output name.
    /// </summary>
    public const string DefaultOutputName = "Result";

    /// <summary>
    /// Records an activity's output.
    /// </summary>
    /// <param name="activityExecutionContext">The activity execution context.</param>
    /// <param name="outputValue">The output value.</param>
    public void Record(ActivityExecutionContext activityExecutionContext, object? outputValue)
    {
        Record(activityExecutionContext, null, outputValue);
    }

    /// <summary>
    /// Records an activity's output.
    /// </summary>
    /// <param name="activityExecutionContext">The activity execution context.</param>
    /// <param name="outputName">The name of the output. Defaults to "Result"</param>
    /// <param name="outputValue">The output value.</param>
    public void Record(ActivityExecutionContext activityExecutionContext, string? outputName, object? outputValue)
    {
        var activityId = activityExecutionContext.Activity.Id;
        var activityInstanceId = activityExecutionContext.Id;
        var containerId = activityExecutionContext.ParentActivityExecutionContext?.Id ?? activityExecutionContext.WorkflowExecutionContext.Id;

        outputName ??= DefaultOutputName;

        // Inspect the output descriptor to see if the specified output name matches any PropertyInfo's name.
        // If so, use that descriptor's name instead.
        var outputDescriptor = activityExecutionContext.ActivityDescriptor.Outputs.FirstOrDefault(x => x.PropertyInfo?.Name == outputName);

        if (outputDescriptor != null)
            outputName = outputDescriptor.Name;

        var record = new ActivityOutputRecord(containerId, activityId, activityInstanceId, outputName, outputValue);

        _recordsByActivityInstanceIdAndOutputName[CreateActivityInstanceIdLookupKey(activityInstanceId, outputName)] = record;

        var scopedRecordsKey = CreateActivityIdLookupKey(activityId, outputName);

        if (!_recordsByActivityIdAndOutputName.TryGetValue(scopedRecordsKey, out var scopedRecords))
        {
            scopedRecords = new();
            _recordsByActivityIdAndOutputName[scopedRecordsKey] = scopedRecords;
        }

        scopedRecords.Add(record);
    }

    /// <summary>
    ///  Finds all output records for the specified activity ID and output name.
    /// </summary>
    public IEnumerable<ActivityOutputRecord> FindMany(string activityId, string? outputName = null)
    {
        var key = CreateActivityIdLookupKey(activityId, outputName);
        return _recordsByActivityIdAndOutputName.TryGetValue(key, out var records) ? records : Enumerable.Empty<ActivityOutputRecord>();
    }

    /// <summary>
    /// Gets the output value for the specified activity ID.
    /// </summary>
    /// <param name="activityId">The activity ID.</param>
    /// <param name="outputName">Name of the output.</param>
    /// <returns>The output value.</returns>
    public object? FindOutputByActivityId(string activityId, string? outputName = null)
    {
        var key = CreateActivityIdLookupKey(activityId, outputName);
        return !_recordsByActivityIdAndOutputName.TryGetValue(key, out var records)
            ? null
            : records.LastOrDefault()?.Value; // Always return the last value.
    }

    /// <summary>
    /// Gets the most recent output value that the specified activity recorded in one of the specified containers.
    /// </summary>
    /// <param name="activityId">The activity ID.</param>
    /// <param name="outputName">Name of the output, or <c>null</c> for <see cref="DefaultOutputName"/>.</param>
    /// <param name="containerIds">The IDs of the containers to look in, usually the reader's own activity execution context and its ancestors.</param>
    /// <param name="value">The output value, which can be <c>null</c> when the activity recorded <c>null</c>.</param>
    /// <returns><c>true</c> if the activity recorded the output in one of the specified containers; otherwise, <c>false</c>.</returns>
    public bool TryFindOutputByActivityId(string activityId, string? outputName, IEnumerable<string> containerIds, out object? value)
    {
        ArgumentNullException.ThrowIfNull(containerIds);
        var key = CreateActivityIdLookupKey(activityId, outputName);

        if (_recordsByActivityIdAndOutputName.TryGetValue(key, out var records))
        {
            var containerIdSet = containerIds as ISet<string> ?? containerIds.ToHashSet();

            // Records are in recording order, so the last matching one is the most recent.
            for (var i = records.Count - 1; i >= 0; i--)
            {
                if (containerIdSet.Contains(records[i].ContainerId))
                {
                    value = records[i].Value;
                    return true;
                }
            }
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Gets the output value for the specified activity instance ID.
    /// </summary>
    /// <param name="activityInstanceId">The activity instance ID.</param>
    /// <param name="outputName"></param>
    /// <returns>The output value.</returns>
    public object? FindOutputByActivityInstanceId(string activityInstanceId, string? outputName = null)
    {
        var key = CreateActivityInstanceIdLookupKey(activityInstanceId, outputName);
        return !_recordsByActivityInstanceIdAndOutputName.TryGetValue(key, out var record)
            ? null
            : record.Value;
    }

    private string CreateActivityIdLookupKey(string activityId, string? outputName) => $"{activityId}:{outputName ?? DefaultOutputName}";
    private string CreateActivityInstanceIdLookupKey(string activityInstanceId, string? outputName) => $"{activityInstanceId}:{outputName ?? DefaultOutputName}";
}