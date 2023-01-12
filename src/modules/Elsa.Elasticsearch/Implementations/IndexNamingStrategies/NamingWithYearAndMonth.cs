using Elsa.Elasticsearch.Services;

namespace Elsa.Elasticsearch.Implementations.IndexNamingStrategies;

public class NamingWithYearAndMonth : IIndexNamingStrategy
{
    public string GenerateName(string aliasName)
    {
        var now = DateTime.Now;
        var month = now.ToString("MM");
        var year = now.Year;

        return aliasName + "-" + year + "-" + month;
    }
}