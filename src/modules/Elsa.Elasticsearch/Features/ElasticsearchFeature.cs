using Elastic.Clients.Elasticsearch;
using Elsa.Elasticsearch.Extensions;
using Elsa.Elasticsearch.HostedServices;
using Elsa.Elasticsearch.Modules.Management;
using Elsa.Elasticsearch.Modules.Runtime;
using Elsa.Elasticsearch.Options;
using Elsa.Elasticsearch.Services;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Microsoft.Extensions.Configuration;
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

    /// <inheritdoc />
    public override void ConfigureHostedServices()
    {
        Module.ConfigureHostedService<ConfigureElasticsearchClientHostedService>(-2);

        if (EnableRollover) 
            Module.ConfigureHostedService<ConfigureIndexRolloverHostedService>(-1);
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services.Configure(Options);
        Services.AddSingleton(sp => new ElasticsearchClient(GetSettings(sp)));
        Services.AddSingleton<IElasticConfiguration, ExecutionLogConfiguration>();
        Services.AddSingleton<IElasticConfiguration, WorkflowInstanceConfiguration>();
    }

    private static ElasticsearchClientSettings GetSettings(IServiceProvider serviceProvider)
    {
        var options = serviceProvider.GetRequiredService<IOptions<ElasticsearchOptions>>().Value;
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var configs = serviceProvider.GetServices<IElasticConfiguration>();
        var url = configuration.GetConnectionString(options.Endpoint) ?? options.Endpoint;
        var settings = new ElasticsearchClientSettings(new Uri(url)).ConfigureAuthentication(options);

        foreach (var config in configs) 
            config.ConfigureClientSettings(settings);

        return settings;
    }
}