namespace Elsa.Common;

/// <summary>
/// Specifies dependencies for a task implementation. Tasks with dependencies will be executed after their dependencies have completed.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class TaskDependencyAttribute(Type dependencyTaskType) : Attribute
{
    /// <summary>
    /// The type of the task that must complete before this task can execute.
    /// </summary>
    public Type DependencyTaskType { get; } = dependencyTaskType;
}
