using Elasticsearch.Net;
using Elsa.Elasticsearch.Options;
using Elsa.Elasticsearch.Services;
using Nest;

namespace Elsa.Elasticsearch.Extensions;

public static class ConnectionSettingsExtensions
{
    public static ConnectionSettings ConfigureAuthentication(this ConnectionSettings settings, ElasticsearchOptions options)
    {
        if (!string.IsNullOrEmpty(options.ApiKey))
        {
            settings.ApiKeyAuthentication(new ApiKeyAuthenticationCredentials(options.ApiKey));
        }
        else if (!string.IsNullOrEmpty(options.Username) && !string.IsNullOrEmpty(options.Password))
        {
            settings.BasicAuthentication(options.Username, options.Password);
        }

        return settings;
    }
    
    public static ConnectionSettings ConfigureMapping(this ConnectionSettings settings, IDictionary<string,string> indexConfig)
    {
        var configs = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(p => typeof(IElasticConfiguration).IsAssignableFrom(p) && p.IsClass);
        
        foreach (var config in configs)
        {
            var configInstance = (IElasticConfiguration)Activator.CreateInstance(config)!;
            configInstance.Apply(settings, indexConfig);
        }

        return settings;
    }
}