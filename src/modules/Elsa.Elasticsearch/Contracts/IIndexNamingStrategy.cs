namespace Elsa.Elasticsearch.Contracts;

/// <summary>
/// Represents a naming strategy to use when creating an index name from an alias.
/// </summary>
public interface IIndexNamingStrategy
{
    /// <summary>
    /// Returns an index name from the specified alias.
    /// </summary>
    string GenerateName(string aliasName);
}