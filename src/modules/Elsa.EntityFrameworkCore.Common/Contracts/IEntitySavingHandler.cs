using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Elsa.EntityFrameworkCore;

/// <summary>
/// Represents handler for entities before saving changes.
/// </summary>
public interface IEntitySavingHandler
{
    /// <summary>
    /// Handles the entity before saving changes.
    /// </summary>
    ValueTask HandleAsync(ElsaDbContextBase dbContext, EntityEntry entry, CancellationToken cancellationToken = default);
}