namespace Elsa.Common.RecurringTasks;

/// <summary>
/// Marks a recurring task whose single execution covers every tenant. The recurring task scheduler runs such a task
/// once per cycle, in the context of one active tenant, instead of once for every tenant.
/// </summary>
/// <remarks>
/// The task runs under the default tenant when it is active, otherwise under the active tenant with the lowest ID
/// (ordinal comparison). Tenants that are stopping are skipped. The attribute is inherited by subclasses.
/// </remarks>
[AttributeUsage(AttributeTargets.Class)]
public sealed class RunOnceForAllTenantsAttribute : Attribute;
