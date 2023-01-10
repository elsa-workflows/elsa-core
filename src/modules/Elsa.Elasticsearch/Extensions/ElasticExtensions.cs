using Elasticsearch.Net;
using Elsa.Elasticsearch.Common;
using Elsa.Elasticsearch.Models;
using Elsa.Elasticsearch.Options;
using Elsa.Elasticsearch.Services;
using Nest;
using Index = Nest.Index;

namespace Elsa.Elasticsearch.Extensions;

public static class ElasticExtensions
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
    
    public static ConnectionSettings ConfigureMapping(this ConnectionSettings settings, IDictionary<Type,string> indexConfig)
    {
        foreach (var config in Utils.GetElasticConfigurationTypes())
        {
            var configInstance = (IElasticConfiguration)Activator.CreateInstance(config)!;
            configInstance.Apply(settings, indexConfig);
        }

        return settings;
    }
    
    public static void ConfigureAliasNaming(this ElasticClient client, IDictionary<Type,string> aliasConfig, IndexRolloverStrategy strategy)
    {
        var namingStrategy = (IIndexNamingStrategy)Activator.CreateInstance(strategy.IndexNamingStrategy, args: client)!;
        namingStrategy.Apply(Utils.GetElasticDocumentTypes(), aliasConfig);
    }
}