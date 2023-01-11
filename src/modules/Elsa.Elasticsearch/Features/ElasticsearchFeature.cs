using Elastic.Clients.Elasticsearch;
using Elsa.Elasticsearch.Extensions;
using Elsa.Elasticsearch.HostedServices;
using Elsa.Elasticsearch.Models;
using Elsa.Elasticsearch.Options;
using Elsa.Elasticsearch.Services;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Elasticsearch.Features;

public class ElasticsearchFeature : FeatureBase
{
    public ElasticsearchFeature(IModule module) : base(module)
    {
    }
    
    internal ElasticsearchOptions Options { get; set; } = new();
    internal IDictionary<Type, string> IndexConfig { get; set; } = IElasticConfiguration.GetDefaultIndexConfig();
    internal IndexRolloverStrategy? IndexRolloverStrategy { get; set; }

    public override void ConfigureHostedServices()
    {
        Module.ConfigureHostedService<ConfigureMappingHostedService>(-1);

        if (IndexRolloverStrategy != null)
        {
            Module.ConfigureHostedService<ConfigureIndexRolloverHostedService>(-1);
        }
    }

    public override void Apply()
    {
        var elasticClient = new ElasticsearchClient(GetSettings());
        Services.AddSingleton(elasticClient);

        if (IndexRolloverStrategy != null)
        {
            elasticClient.ConfigureAliases(IndexConfig, IndexRolloverStrategy);

            var namingInstance =
                (IIndexNamingStrategy) Activator.CreateInstance(IndexRolloverStrategy.IndexNamingStrategy)!;
            Services.AddSingleton<IIndexNamingStrategy>(_ => namingInstance);

            var rolloverInstance =
                (IIndexRolloverStrategy) Activator.CreateInstance(IndexRolloverStrategy.Value, elasticClient,
                    namingInstance)!;
            Services.AddSingleton<IIndexRolloverStrategy>(_ => rolloverInstance);
        }
    }

    private ElasticsearchClientSettings GetSettings()
    {
        return new ElasticsearchClientSettings(new Uri(Options.Endpoint))
            .ConfigureAuthentication(Options)
            .ConfigureMapping(IndexConfig);
    }
}