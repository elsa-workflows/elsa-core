namespace Elsa.Elasticsearch.Services;

public interface IRolloverStrategy
{
    void Apply(IEnumerable<Type> types, IDictionary<Type,string> aliasConfig);
}