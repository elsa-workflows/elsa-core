namespace Elsa.ModularPersistence.MongoDb.Options;

/// <summary>
/// Determines how storage units map to MongoDB collections.
/// </summary>
public enum MongoDbCollectionStrategy
{
    /// <summary>
    /// Store all document types in one collection and use the Type field for discrimination.
    /// </summary>
    SharedCollection = 0,

    /// <summary>
    /// Store each document type in its own collection.
    /// </summary>
    CollectionPerType = 1
}
