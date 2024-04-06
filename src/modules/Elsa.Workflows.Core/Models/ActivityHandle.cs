namespace Elsa.Workflows.Models;

/// <summary>
/// Represents a handle to an activity.
/// </summary>
public class ActivityHandle
{
    public static ActivityHandle FromActivityId(string activityId) => new() { ActivityId = activityId };
    public static ActivityHandle FromActivityNodeId(string activityNodeId) => new() { ActivityNodeId = activityNodeId };
    public static ActivityHandle FromActivityInstanceId(string activityInstanceId) => new() { ActivityInstanceId = activityInstanceId };
    public static ActivityHandle FromActivityHash(string activityHash) => new() { ActivityHash = activityHash };
    public string? ActivityId { get; init; }
    public string? ActivityNodeId { get; init;}
    public string? ActivityInstanceId { get; init;}
    public string? ActivityHash { get; init;}
}