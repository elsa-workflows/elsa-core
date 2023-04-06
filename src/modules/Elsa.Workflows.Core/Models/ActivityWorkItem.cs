namespace Elsa.Workflows.Core.Models;

public class ActivityWorkItem
{
    public ActivityWorkItem(string activityId, Func<ValueTask> execute, object? tag = default)
    {
        ActivityId = activityId;
        Execute = execute;
        Tag = tag;
    }

    public string ActivityId { get; }
    public Func<ValueTask> Execute { get; }
    public object? Tag { get; set; }
}