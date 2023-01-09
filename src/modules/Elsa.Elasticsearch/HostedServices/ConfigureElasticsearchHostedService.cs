using Elsa.Elasticsearch.Scheduling;
using Elsa.Jobs.Schedules;
using Elsa.Jobs.Services;
using Elsa.Workflows.Management.Entities;
using Microsoft.Extensions.Hosting;
using Nest;

namespace Elsa.Elasticsearch.HostedServices;

public class ConfigureElasticsearchHostedService : IHostedService
{
    private readonly ElasticClient _elasticClient;
    private readonly IJobScheduler _jobScheduler;

    public ConfigureElasticsearchHostedService(ElasticClient elasticClient, IJobScheduler jobScheduler)
    {
        _elasticClient = elasticClient;
        _jobScheduler = jobScheduler;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await FlattenProperties(cancellationToken);

        await ScheduleIndexAndAliasConfiguration(cancellationToken);
    }

    private async Task ScheduleIndexAndAliasConfiguration(CancellationToken cancellationToken)
    {
        var job = new ConfigureElasticIndicesJob();
        var schedule = new CronSchedule
        {
            // At the start of every month
            CronExpression = "*/5 * * * *"
        };
        
        await _jobScheduler.ScheduleAsync(job, GetType().Name, schedule, cancellationToken: cancellationToken);
    }

    private async Task FlattenProperties(CancellationToken cancellationToken)
    {
        await _elasticClient.Indices.PutMappingAsync<WorkflowInstance>(
            descriptor => descriptor
                .Properties(p => p
                    .Flattened(d => d
                        .Name(p => p.WorkflowState.Properties))),
            cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}