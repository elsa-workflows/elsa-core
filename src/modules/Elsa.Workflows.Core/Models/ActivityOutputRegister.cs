using Elsa.Extensions;

namespace Elsa.Workflows.Core.Models;

/// <summary>
/// Stores activity output.
/// </summary>
public class ActivityOutputRegister
{
    /// <summary>
    /// The default output name.
    /// </summary>
    public const string DefaultOutputName = "Result";
    
    private readonly Dictionary<string, IDictionary<string, object>> _nodeIdLookup = new();
    private readonly Dictionary<string, IDictionary<string, object>> _activityIdLookup = new();
    private readonly Dictionary<string, IDictionary<string, object>> _activityInstanceIdLookup = new();
    
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
        var nodeId = activityExecutionContext.NodeId;
        var activityId = activityExecutionContext.Activity.Id;
        var activityInstanceId = activityExecutionContext.Id;
        var record = (_nodeIdLookup.TryGetValue(nodeId, out var value) ? value : default) ?? new Dictionary<string, object>();

        record[outputName ?? DefaultOutputName] = outputValue!;
        
        _nodeIdLookup[nodeId] = record;
        _activityIdLookup[activityId] = record;
        _activityInstanceIdLookup[activityInstanceId] = record;
    }

    /// <summary>
    /// Gets the output value for the specified node ID.
    /// </summary>
    /// <param name="nodeId">The node ID.</param>
    /// <param name="outputName">Name of the output.</param>
    /// <returns>The output value.</returns>
    public object? FindOutputByNodeId(string nodeId, string? outputName = default)
    {
        var record = _nodeIdLookup.TryGetValue(nodeId, out var value) ? value : default;
        return record?.GetValueOrDefault<object>(outputName ?? DefaultOutputName);
    }

    /// <summary>
    /// Gets the output value for the specified activity ID.
    /// </summary>
    /// <param name="activityId">The activity ID.</param>
    /// <param name="outputName">Name of the output.</param>
    /// <returns>The output value.</returns>
    public object? FindOutputByActivityId(string activityId, string? outputName = default)
    {
        var record = _activityIdLookup.TryGetValue(activityId, out var value) ? value : default;
        return record?.GetValueOrDefault<object>(outputName ?? DefaultOutputName);
    }

    /// <summary>
    /// Gets the output value for the specified activity instance ID.
    /// </summary>
    /// <param name="activityInstanceId">The activity instance ID.</param>
    /// <param name="outputName"></param>
    /// <returns>The output value.</returns>
    public object? FindOutputByActivityInstanceId(string activityInstanceId, string? outputName = default)
    {
        var record = _activityInstanceIdLookup.TryGetValue(activityInstanceId, out var value) ? value : default;
        return record?.GetValueOrDefault<object>(outputName ?? DefaultOutputName);
    }
}