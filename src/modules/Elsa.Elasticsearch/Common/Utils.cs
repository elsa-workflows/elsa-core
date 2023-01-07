using Elsa.Elasticsearch.Services;

namespace Elsa.Elasticsearch.Common;

public static class Utils
{
    public static string GenerateIndexName(string aliasName)
    {
        var month = DateTime.Now.ToString("MM");
        var year = DateTime.Now.Year;
        
        return aliasName + "-" + year + "-" + month;
    }
    
    public static IEnumerable<Type> GetElasticDocumentTypes() =>
        AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(p => typeof(IElasticConfiguration).IsAssignableFrom(p) && p.IsClass);
}