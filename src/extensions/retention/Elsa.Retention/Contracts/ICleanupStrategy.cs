namespace Elsa.Retention.Contracts;

public interface ICleanupStrategy
{
    Task Cleanup(ICollection<object> collection);
}

/// <summary>
///     A strategy responsible for cleaning up the <see cref="TEntity" />
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public interface ICleanupStrategy<TEntity> : ICleanupStrategy
{
    /// <summary>
    ///     Cleans up the given entity
    /// </summary>
    /// <param name="collection"></param>
    /// <returns></returns>
    Task ICleanupStrategy.Cleanup(ICollection<object> collection)
    {
        return Cleanup(collection.Cast<TEntity>().ToList());
    }

    /// <summary>
    ///     Cleans up the given entities
    /// </summary>
    /// <param name="collection"></param>
    /// <returns></returns>
    Task Cleanup(ICollection<TEntity> collection);
}