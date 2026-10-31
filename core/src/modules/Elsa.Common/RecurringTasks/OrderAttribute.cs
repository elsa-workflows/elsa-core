namespace Elsa.Common.RecurringTasks;

/// <summary>
/// Configures a task with a priority.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class OrderAttribute(float order) : Attribute
{
    public float Order { get; } = order;
}