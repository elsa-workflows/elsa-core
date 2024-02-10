using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Elsa.EntityFrameworkCore.Common.Contracts;

/// <summary>
/// Represents an interface for DbContext strategies that are executed when the model is being created.
/// </summary>
public interface IModelCreatingDbContextStrategy : IDbContextStrategy
{
    /// <summary>
    /// Determines whether the strategy can be executed for the given entity.
    /// </summary>
    bool CanExecute(ModelBuilder modelBuilder, IMutableEntityType mutableEntity);

    /// <summary>
    /// Executes the strategy for the given entity.
    /// </summary>
    void Execute(ModelBuilder modelBuilder, IMutableEntityType mutableEntity);
}
