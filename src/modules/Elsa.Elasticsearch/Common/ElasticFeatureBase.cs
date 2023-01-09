using Elasticsearch.Net;
using Elsa.Elasticsearch.Extensions;
using Elsa.Elasticsearch.HostedServices;
using Elsa.Elasticsearch.Models;
using Elsa.Elasticsearch.Options;
using Elsa.Elasticsearch.Scheduling;
using Elsa.Elasticsearch.Services;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;
using Nest;

namespace Elsa.Elasticsearch.Common;

public abstract class ElasticFeatureBase : FeatureBase
{
    public ElasticFeatureBase(IModule module) : base(module)
    {
    }
    
    internal ElasticsearchOptions Options { get; set; } = new();
    internal IDictionary<Type, string> IndexConfig { get; set; } = IElasticConfiguration.GetDefaultIndexConfig();
    internal IndexRolloverStrategy? IndexRolloverStrategy { get; set; }

    public override void ConfigureHostedServices()
    {
        Module.ConfigureHostedService<ConfigureElasticsearchHostedService>(-1);
    }

    public override void Apply()
    {
        if (Services.Any(x => x.ServiceType == typeof(ElasticClient))) return;
        
        var elasticClient = new ElasticClient(GetSettings());
        
        if (IndexRolloverStrategy != null)
        {
            elasticClient.ApplyRolloverStrategy(IndexConfig, IndexRolloverStrategy!);
        }
        
        Services.AddSingleton(elasticClient);
    }

    private ConnectionSettings GetSettings()
    {
        return new ConnectionSettings(new Uri(Options.Endpoint))
            .ConfigureAuthentication(Options)
            .ConfigureMapping(IndexConfig);
    }

    protected void AddStore<TModel, TStore>() where TModel : class where TStore : class
    {
        Services
            .AddSingleton<ElasticStore<TModel>>()
            .AddSingleton<TStore>();
    }
}