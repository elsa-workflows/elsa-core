using Elsa.Alterations.Core.Entities;
using Elsa.Alterations.Core.Filters;

namespace Elsa.Alterations.Core.Contracts;

/// <summary>
/// A store for alteration jobs.
/// </summary>
public interface IAlterationJobStore
{
    /// <summary>
    /// Saves the specified alteration job.
    /// </summary>
    Task SaveAsync(AlterationJob job, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Saves the specified alteration jobs.
    /// </summary>
    Task SaveManyAsync(IEnumerable<AlterationJob> jobs, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the alteration job matching the specified filter.
    /// </summary>
    Task<AlterationJob?> FindAsync(AlterationJobFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds all alteration jobs matching the specified filter.
    /// </summary>
    Task<IEnumerable<AlterationJob>> FindManyAsync(AlterationJobFilter filter, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Returns the number of alteration jobs matching the specified filter.
    /// </summary>
    Task<long> CountAsync(AlterationJobFilter filter, CancellationToken cancellationToken = default);
}