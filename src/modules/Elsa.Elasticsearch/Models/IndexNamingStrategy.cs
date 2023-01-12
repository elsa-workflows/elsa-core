using Elsa.Elasticsearch.Implementations.IndexNamingStrategies;
using JetBrains.Annotations;

namespace Elsa.Elasticsearch.Models;

[PublicAPI]
public class IndexNamingStrategy
{
    private IndexNamingStrategy(Type value) { Value = value; }

    public Type Value { get; private set; }

    public static IndexNamingStrategy NamingWithYearAndMonth => new (typeof(NamingWithYearAndMonth));

    /// <inheritdoc />
    public override string ToString() => Value.Name;
}