using Elsa.Elasticsearch.Services;

namespace Elsa.Elasticsearch.Common;

public static class Utils
{
    public static string GenerateIndexName(string aliasName)
    {
        var now = DateTime.Now;
        var month = now.ToString("MM");
        var year = now.Year;
        var day = now.Day;
        var hour = now.Hour;
        var minute = now.Minute;

        return aliasName + "-" + year + "-" + month + "-" + day + hour + minute;
    }

    public static IEnumerable<Type> GetElasticConfigurationTypes() =>
        AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(p => 
                typeof(IElasticConfiguration).IsAssignableFrom(p) && 
                p is {IsClass: true, IsAbstract: false});

    public static IEnumerable<Type> GetElasticDocumentTypes() =>
        GetElasticConfigurationTypes()
            .Select(t => t.BaseType!.GenericTypeArguments.First()).ToList();
    
    public static IDictionary<Type, string> ResolveAliasConfig(
        IDictionary<Type, string> defaultConfig, 
        IDictionary<string, string>? option1, 
        IDictionary<string, string>? option2)
    {
        var types = GetElasticDocumentTypes();
        
        return option1?.Select(kvp => new KeyValuePair<Type, string>(types.First(t => t.Name == kvp.Key), kvp.Value))
                   .ToDictionary(x => x.Key, x => x.Value) ?? 
               option2?.Select(kvp => new KeyValuePair<Type, string>(types.First(t => t.Name == kvp.Key), kvp.Value))
                   .ToDictionary(x => x.Key, x => x.Value) ??
               defaultConfig;
    }
}