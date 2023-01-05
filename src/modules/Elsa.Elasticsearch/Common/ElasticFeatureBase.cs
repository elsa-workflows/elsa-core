using Elasticsearch.Net;
using Elsa.Elasticsearch.Options;
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
            .SetupAuthentication(Options)
            .SetupMappingsAndIndices();
    }

    protected void AddStore<TModel, TStore>() where TModel : class where TStore : class
    {
        Services
            .AddSingleton<ElasticStore<TModel>>()
            .AddSingleton<TStore>();
    }
}