using Elasticsearch.Net;
using Elsa.Elasticsearch.Extensions;
using Elsa.Elasticsearch.Options;
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
    internal IDictionary<string, string> AliasConfig { get; set; } = IElasticConfiguration.GetDefaultAliasConfig();

    public override void Apply()
    {
        if (Services.Any(x => x.ServiceType == typeof(ElasticClient))) return;
        
        var elasticClient = new ElasticClient(GetSettings());
        elasticClient.ConfigureIndicesAndAliases(AliasConfig);
        Services.AddSingleton(elasticClient);
    }

    private ConnectionSettings GetSettings()
    {
        return new ConnectionSettings(new Uri(Options.Endpoint))
            .ConfigureAuthentication(Options)
            .ConfigureMapping(AliasConfig);
    }

    protected void AddStore<TModel, TStore>() where TModel : class where TStore : class
    {
        Services
            .AddSingleton<ElasticStore<TModel>>()
            .AddSingleton<TStore>();
    }
}