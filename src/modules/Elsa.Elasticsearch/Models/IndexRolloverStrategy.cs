using Elsa.Elasticsearch.Implementations.RolloverStrategies;

namespace Elsa.Elasticsearch.Models;

public class IndexRolloverStrategy
{
    private IndexRolloverStrategy(Type value) { Value = value; }

    public Type Value { get; private set; }

    public static IndexRolloverStrategy RolloverOnMonthlyBasis => new (typeof(RolloverOnMonthlyBasis));

    public override string ToString()
    {
        return Value.Name;
    }
}