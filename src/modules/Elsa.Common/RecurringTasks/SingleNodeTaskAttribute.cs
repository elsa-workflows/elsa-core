namespace Elsa.Common.RecurringTasks;

/// <summary>
/// Configures a task to be executed on a single node in a multi-node environment.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class SingleNodeTaskAttribute : Attribute
{
    public SingleNodeTaskAttribute() : this(SingleNodeTaskScope.Tenant)
    {
    }

    public SingleNodeTaskAttribute(SingleNodeTaskScope scope)
    {
        Scope = scope;
    }

    /// <summary>
    /// Gets the scope within which the task is mutually exclusive.
    /// </summary>
    public SingleNodeTaskScope Scope { get; }
}
