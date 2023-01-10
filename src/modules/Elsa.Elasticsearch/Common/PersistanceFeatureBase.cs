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

public abstract class ElasticPersistanceFeatureBase : FeatureBase
{
    public ElasticPersistanceFeatureBase(IModule module) : base(module)
    {
    }

    protected void AddStore<TModel, TStore>() where TModel : class where TStore : class
    {
        Services
            .AddSingleton<ElasticStore<TModel>>()
            .AddSingleton<TStore>();
    }
}