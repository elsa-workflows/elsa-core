namespace Elsa.Workflows.Core.Models;

public class ActivityWorkItem
{
    public ActivityWorkItem(string activityNodeId, Func<ValueTask> execute, object? tag = default)
    {
        ActivityNodeId = activityNodeId;
        Execute = execute;
        Tag = tag;
    }

    public string ActivityNodeId { get; }
    public Func<ValueTask> Execute { get; }
    public object? Tag { get; set; }
}