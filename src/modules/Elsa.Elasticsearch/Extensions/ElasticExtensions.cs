using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Transport;
using Elsa.Elasticsearch.Common;
using Elsa.Elasticsearch.Models;
using Elsa.Elasticsearch.Options;
using Elsa.Elasticsearch.Services;

namespace Elsa.Elasticsearch.Extensions;

public static class ElasticExtensions
{
    public static ElasticsearchClientSettings ConfigureAuthentication(this ElasticsearchClientSettings settings, ElasticsearchOptions options)
    {
        if (!string.IsNullOrEmpty(options.ApiKey))
        {
            settings.Authentication(new ApiKey(options.ApiKey));
        }
        else if (!string.IsNullOrEmpty(options.Username) && !string.IsNullOrEmpty(options.Password))
        {
            settings.Authentication(new BasicAuthentication(options.Username, options.Password));
        }

        return settings;
    }
    
    public static ElasticsearchClientSettings ConfigureMapping(this ElasticsearchClientSettings settings, IDictionary<Type,string> indexConfig)
    {
        foreach (var config in Utils.GetElasticConfigurationTypes())
        {
            var configInstance = (IElasticConfiguration)Activator.CreateInstance(config)!;
            configInstance.Apply(settings, indexConfig);
        }

        return settings;
    }
    
    public static void ConfigureAliases(this ElasticsearchClient client, IDictionary<Type,string> aliasConfig, IndexRolloverStrategy strategy)
    {
        var namingStrategy = (IIndexNamingStrategy)Activator.CreateInstance(strategy.IndexNamingStrategy)!;
        
        foreach (var type in Utils.GetElasticDocumentTypes())
        {
            var aliasName = aliasConfig[type];
            var indexName = namingStrategy.GenerateName(aliasName);
            
            var indexExists = client.Indices.Exists(indexName).Exists;
            if (indexExists) continue;
            
            var response = client.Indices.Create(indexName, c => c
                .Aliases(a => a.Add(aliasName, new Alias {IsWriteIndex = true})));
            
            if (response.IsValidResponse) continue;
            response.TryGetOriginalException(out var exception);
            if(exception != null)
                throw exception;
        }
    }
}