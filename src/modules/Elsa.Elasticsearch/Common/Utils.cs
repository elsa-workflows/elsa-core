using Elsa.Elasticsearch.Services;

namespace Elsa.Elasticsearch.Common;

public static class Utils
{
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
        IDictionary<string, string>? userDefinedConfig)
    {
        var types = GetElasticDocumentTypes();
        
        return userDefinedConfig?.Select(kvp => 
                new KeyValuePair<Type, string>(types.First(t => t.Name == kvp.Key), kvp.Value))
            .ToDictionary(x => x.Key, x => x.Value) ?? defaultConfig;
    }
}