using Elsa.Retention.Models;

namespace Elsa.Retention.Contracts;

/// <summary>
///     Defines which workflows should be retained and how they are retained
/// </summary>
public interface IRetentionPolicy
{
    /// <summary>
    ///     The name of this policy
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     The workflow instance filter
    /// </summary>
    Func<IServiceProvider, RetentionWorkflowInstanceFilter> FilterFactory { get; }

    /// <summary>
    ///     A marker type for which <see cref="ICleanupStrategy{TEntity}" /> has to be used
    /// </summary>
    Type CleanupStrategy { get; }
}