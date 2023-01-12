using Elastic.Clients.Elasticsearch;
using Elsa.Elasticsearch.Extensions;
using Elsa.Elasticsearch.HostedServices;
using Elsa.Elasticsearch.Implementations.RolloverStrategies;
using Elsa.Elasticsearch.Models;
using Elsa.Elasticsearch.Options;
using Elsa.Elasticsearch.Services;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Elasticsearch.Features;

/// <summary>
/// Configures Elasticsearch.
/// </summary>
public class ElasticsearchFeature : FeatureBase
{
    /// <inheritdoc />
    public ElasticsearchFeature(IModule module) : base(module)
    {
    }

    /// <summary>
    /// A delegate that configures Elasticsearch.
    /// </summary>
    public Action<ElasticsearchOptions> Options { get; set; } = _ => { };

    /// <summary>
    /// True to enable index name rollovers, false otherwise.
    /// </summary>
    public bool EnableRollover { get; set; }
    
    /// <summary>
    /// A delegate that resolves the <see cref="IIndexRolloverStrategy"/> to use when rolling over.
    /// </summary>
    public Func<IServiceProvider, IIndexRolloverStrategy> IndexRolloverStrategy { get; set; } = sp => sp.GetRequiredService<MonthlyRollover>();

    /// <inheritdoc />
    public override void ConfigureHostedServices()
    {
        Module.ConfigureHostedService<ConfigureMappingHostedService>(-1);

        if (EnableRollover) 
            Module.ConfigureHostedService<ConfigureIndexRolloverHostedService>(-1);
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services.Configure(Options);
        
        Services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<ElasticsearchOptions>>().Value;
            var client = new ElasticsearchClient(GetSettings(options));

            return client;
        });

        Services.AddSingleton(IndexRolloverStrategy);
    }

    private ElasticsearchClientSettings GetSettings(ElasticsearchOptions options)
    {
        return new ElasticsearchClientSettings(options.Endpoint)
            .ConfigureAuthentication(options)
            .ConfigureMapping(IndexConfig);
    }
}