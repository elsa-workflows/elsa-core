using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Elsa.EntityFrameworkCore.Common.Contracts;

/// <summary>
/// Represents handler for entity model creation.
/// </summary>
public interface IEntityModelCreatingHandler
{
    /// <summary>
    /// Handles the entity model being created.
    /// </summary>
    void Handle(ElsaDbContextBase dbContext, ModelBuilder modelBuilder, IMutableEntityType entityType);
}