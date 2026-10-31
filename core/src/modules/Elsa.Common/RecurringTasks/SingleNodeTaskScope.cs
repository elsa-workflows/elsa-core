namespace Elsa.Common.RecurringTasks;

/// <summary>
/// Determines the scope within which a task marked with <see cref="SingleNodeTaskAttribute"/> is mutually exclusive.
/// </summary>
public enum SingleNodeTaskScope
{
    /// <summary>
    /// The task runs on a single node per tenant. Different tenants may run the task at the same time.
    /// </summary>
    Tenant,

    /// <summary>
    /// The task runs on a single node for the whole host. Only one tenant at a time may run the task.
    /// Use this for tasks that guard a resource shared by every tenant.
    /// </summary>
    Host
}
