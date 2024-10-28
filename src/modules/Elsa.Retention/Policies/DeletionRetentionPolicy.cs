using Elsa.Retention.Contracts;
using Elsa.Retention.Models;

namespace Elsa.Retention.Policies;

/// <summary>
///     A policy that will delete the workflow instance and its related entities
/// </summary>
public class DeletionRetentionPolicy : IRetentionPolicy
{
    public DeletionRetentionPolicy(string name, Func<IServiceProvider, RetentionWorkflowInstanceFilter> filter)
    {
        Name = name;
        FilterFactory = filter;
    }

    public string Name { get; }
    public Func<IServiceProvider, RetentionWorkflowInstanceFilter> FilterFactory { get; }

    public Type CleanupStrategy => typeof(IDeletionCleanupStrategy<>);
}