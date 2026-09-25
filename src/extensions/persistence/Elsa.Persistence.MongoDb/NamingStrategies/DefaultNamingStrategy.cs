using Elsa.Persistence.MongoDb.Contracts;

namespace Elsa.Persistence.MongoDb.NamingStrategies;

/// <summary>
/// Returns the same collection name, without modifying it.
/// </summary>
public class DefaultNamingStrategy : ICollectionNamingStrategy
{
    /// <summary>
    /// Returns the same collection name, without modifying it.
    /// </summary>
    public string GetCollectionName(string collectionName)
    {
        return collectionName;
    }
}
