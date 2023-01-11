namespace Elsa.Elasticsearch.Services;

public interface IIndexNamingStrategy
{
    string GenerateName(string aliasName);
}