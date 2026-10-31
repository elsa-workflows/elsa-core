using Elsa.Retention.Contracts;
using Elsa.Retention.Models;

namespace Elsa.Retention.Policies;

/// <summary>
///     A policy that will delete the workflow instance and its related entities
/// </summary>
public class DeletionRetentionPolicy(string name, Func<IServiceProvider, RetentionWorkflowInstanceFilter> filter) : IRetentionPolicy
{
    public string Name { get; } = name;
    public Func<IServiceProvider, RetentionWorkflowInstanceFilter> FilterFactory { get; } = filter;

    public Type CleanupStrategy => typeof(IDeletionCleanupStrategy<>);
}