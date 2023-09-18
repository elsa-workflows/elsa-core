namespace Elsa.Workflows.Core.Models;

/// <summary>
/// Stores activity output.
/// </summary>
public class ActivityOutputRegister
{
    private readonly ICollection<ActivityOutputRecord> _records = new List<ActivityOutputRecord>();

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
        Record(activityExecutionContext, default, outputValue);
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

        _records.Add(record);
    }

    /// <summary>
    /// Finds all output records matching the specified predicate.
    /// </summary>
    public IEnumerable<ActivityOutputRecord> FindMany(Func<ActivityOutputRecord, bool> predicate) => _records.Where(predicate);

    /// <summary>
    /// Gets the output value for the specified activity ID.
    /// </summary>
    /// <param name="activityId">The activity ID.</param>
    /// <param name="outputName">Name of the output.</param>
    /// <returns>The output value.</returns>
    public object? FindOutputByActivityId(string activityId, string? outputName = default)
    {
        var record = _records.FirstOrDefault(x => x.ActivityId == activityId && x.OutputName == (outputName ?? DefaultOutputName));
        return record?.Value;
    }

    /// <summary>
    /// Gets the output value for the specified activity instance ID.
    /// </summary>
    /// <param name="activityInstanceId">The activity instance ID.</param>
    /// <param name="outputName"></param>
    /// <returns>The output value.</returns>
    public object? FindOutputByActivityInstanceId(string activityInstanceId, string? outputName = default)
    {
        var record = _records.FirstOrDefault(x => x.ActivityInstanceId == activityInstanceId && x.OutputName == (outputName ?? DefaultOutputName));
        return record?.Value;
    }
}