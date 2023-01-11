using Elsa.Elasticsearch.Services;

namespace Elsa.Elasticsearch.Implementations.IndexNamingStrategies;

public class NamingWithYearAndMonth : IIndexNamingStrategy
{
    public string GenerateName(string aliasName)
    {
        var now = DateTime.Now;
        var month = now.ToString("MM");
        var year = now.Year;
        var day = now.Day;
        var hour = now.Hour;
        var minute = now.Minute;

        return aliasName + "-" + year + "-" + month + "-" + day + hour + minute;
    }
}