using Elsa.ModularPersistence.MongoDb.Options;

namespace Elsa.ModularPersistence.MongoDb.Services;

/// <summary>
/// Resolves MongoDB collection names for modular persistence documents.
/// </summary>
public sealed class MongoDbCollectionResolver(MongoDbModularPersistenceOptions options)
{
    public string GetCollectionName(string documentType) =>
        options.CollectionStrategy switch
        {
            MongoDbCollectionStrategy.SharedCollection => options.SharedCollectionName,
            MongoDbCollectionStrategy.CollectionPerType => $"{options.CollectionPerTypePrefix}_{SanitizeCollectionName(documentType)}",
            _ => throw new ArgumentOutOfRangeException(nameof(options.CollectionStrategy), options.CollectionStrategy, "Unknown MongoDB collection strategy.")
        };

    public IReadOnlyCollection<string> GetCollectionNames(IEnumerable<string> documentTypes) =>
        options.CollectionStrategy == MongoDbCollectionStrategy.SharedCollection
            ? [options.SharedCollectionName]
            : documentTypes.Select(GetCollectionName).Distinct(StringComparer.Ordinal).ToArray();

    private static string SanitizeCollectionName(string value)
    {
        var chars = value.Select(x => char.IsLetterOrDigit(x) ? x : '_').ToArray();
        return new string(chars);
    }
}
