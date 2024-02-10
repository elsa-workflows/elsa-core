using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Elsa.EntityFrameworkCore.Common.Contracts;

/// <summary>
/// Represents an interface for DbContext strategies that are executed before saving changes.
/// </summary>
public interface IBeforeSavingDbContextStrategy : IDbContextStrategy
{
    /// <summary>
    /// Determines whether the strategy can be executed for the given entity entry.
    /// </summary>
    bool CanExecute(EntityEntry entityEntry);

    /// <summary>
    /// Executes the strategy for the given entity entry.
    /// </summary>
    void Execute(EntityEntry entityEntry);
}
