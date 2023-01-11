using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Elasticsearch.Common;

public abstract class ElasticPersistenceFeatureBase : FeatureBase
{
    public ElasticPersistenceFeatureBase(IModule module) : base(module)
    {
    }

    protected void AddStore<TModel, TStore>() where TModel : class where TStore : class
    {
        Services
            .AddSingleton<ElasticStore<TModel>>()
            .AddSingleton<TStore>();
    }
}