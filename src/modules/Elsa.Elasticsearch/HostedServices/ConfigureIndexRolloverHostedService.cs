using Elsa.Elasticsearch.Options;
using Elsa.Elasticsearch.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Elsa.Elasticsearch.HostedServices;

/// <summary>
/// Configures a recurring job that applies the rollover strategy.
/// </summary>
public class ConfigureIndexRolloverHostedService : BackgroundService
{
    private readonly IIndexRolloverStrategy _rolloverStrategy;
    private readonly ElasticsearchOptions _options;

    /// <summary>
    /// Constructor.
    /// </summary>
    public ConfigureIndexRolloverHostedService(IIndexRolloverStrategy rolloverStrategy, IOptions<ElasticsearchOptions> options)
    {
        _rolloverStrategy = rolloverStrategy;
        _options = options.Value;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_options.RolloverInterval, stoppingToken);
            await _rolloverStrategy.ApplyAsync(stoppingToken);
        }
    }
}