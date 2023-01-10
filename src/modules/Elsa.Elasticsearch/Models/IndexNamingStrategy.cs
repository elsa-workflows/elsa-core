using Elsa.Elasticsearch.Implementations.IndexNamingStrategies;

namespace Elsa.Elasticsearch.Models;

public class IndexNamingStrategy
{
    private IndexNamingStrategy(Type value) { Value = value; }

    public Type Value { get; private set; }

    public static IndexNamingStrategy NamingWithYearAndMonth => new (typeof(NamingWithYearAndMonth));

    public override string ToString()
    {
        return Value.Name;
    }
}