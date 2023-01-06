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
    internal IDictionary<string,string> IndexConfig { get; set; }

    public override void Apply()
    {
        if (Services.All(x => x.ServiceType != typeof(ElasticClient)))
        {
            Services.AddSingleton(new ElasticClient(GetSettings()));
        }
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