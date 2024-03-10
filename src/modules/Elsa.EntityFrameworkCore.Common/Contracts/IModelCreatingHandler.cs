using Microsoft.EntityFrameworkCore;

namespace Elsa.EntityFrameworkCore.Common.Contracts;

/// <summary>
/// Represents handler for model creation.
/// </summary>
public interface IModelCreatingHandler
{
    /// <summary>
    /// Handles the model being created.
    /// </summary>
    void Handle(ElsaDbContextBase dbContext, ModelBuilder modelBuilder);
}