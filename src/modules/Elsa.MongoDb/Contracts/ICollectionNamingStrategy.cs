namespace Elsa.MongoDb.Contracts;

/// <summary>
/// Represents a naming strategy to use when creating the name of a MongoDB collection.
/// </summary>
public interface ICollectionNamingStrategy
{
    /// <summary>
    /// Returns a collection name from the specified base collection name.
    /// </summary>
    string GetCollectionName(string collectionName);
}
