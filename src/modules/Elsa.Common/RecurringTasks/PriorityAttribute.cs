namespace Elsa.Common.RecurringTasks;

/// <summary>
/// Configures a task with a priority.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class PriorityAttribute(float priority) : Attribute
{
    public float Priority { get; } = priority;
}