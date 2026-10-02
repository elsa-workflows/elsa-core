namespace Elsa.Retention.Contracts;

/// <summary>
///     A strategy responsible for deleting the given entities
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public interface IDeletionCleanupStrategy<TEntity> : ICleanupStrategy<TEntity>
{
}