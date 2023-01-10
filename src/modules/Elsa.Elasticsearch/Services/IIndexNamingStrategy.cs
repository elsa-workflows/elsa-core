namespace Elsa.Elasticsearch.Services;

public interface IIndexNamingStrategy
{
    void Apply(IEnumerable<Type> typesToConfigure, IDictionary<Type, string> aliasConfig);
}