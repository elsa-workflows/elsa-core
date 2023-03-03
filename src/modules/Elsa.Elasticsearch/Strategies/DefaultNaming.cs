using Elsa.Elasticsearch.Contracts;
using Humanizer;

namespace Elsa.Elasticsearch.Strategies;

/// <summary>
/// Returns the alias as the name for the index.
/// </summary>
public class DefaultNaming : IIndexNamingStrategy
{
    /// <inheritdoc />
    public string GenerateName(string aliasName) => aliasName.Kebaberize();
}