using Elsa.Elasticsearch.Extensions;
using Elsa.Elasticsearch.HostedServices;
using Elsa.Elasticsearch.Models;
using Elsa.Elasticsearch.Options;
using Elsa.Elasticsearch.Services;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;
using Nest;

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
        var elasticClient = new ElasticClient(GetSettings());
        
        if (IndexRolloverStrategy != null)
        {
            elasticClient.ConfigureAliasNaming(IndexConfig, IndexRolloverStrategy);

            var typeInstance = (IIndexRolloverStrategy) Activator.CreateInstance(IndexRolloverStrategy.Value, args: elasticClient)!;
            Services.AddSingleton<IIndexRolloverStrategy>(_ => typeInstance);
        }
        
        Services.AddSingleton(elasticClient);
    }

    private ConnectionSettings GetSettings()
    {
        return new ConnectionSettings(new Uri(Options.Endpoint))
            .ConfigureAuthentication(Options)
            .ConfigureMapping(IndexConfig);
    }
}