using System.Reflection.Emit;
using Elsa.Elasticsearch.Implementations.IndexNamingStrategies;
using Elsa.Elasticsearch.Implementations.RolloverStrategies;
using Elsa.Elasticsearch.Services;

namespace Elsa.Elasticsearch.Models;

public class IndexRolloverStrategy
{
    private IndexRolloverStrategy(Type value, Type indexNamingStrategy)
    {
        Value = value;
        IndexNamingStrategy = indexNamingStrategy;
    }

    public Type Value { get; private set; }
    public Type IndexNamingStrategy { get; private set; }

    public static IndexRolloverStrategy RolloverOnMonthlyBasis => new (typeof(RolloverOnMonthlyBasis),typeof(NamingWithYearAndMonth));
}