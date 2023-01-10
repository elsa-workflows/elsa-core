using System.Text.Json.Serialization;
using Elsa.Elasticsearch.Common;
using Elsa.Elasticsearch.Services;
using Elsa.Jobs.Models;
using Microsoft.Extensions.DependencyInjection;
using Job = Elsa.Jobs.Abstractions.Job;

namespace Elsa.Elasticsearch.Scheduling;

public class ConfigureIndexRolloverJob : Job
{
    [JsonConstructor]
    public ConfigureIndexRolloverJob()
    {
    }

    protected override async ValueTask ExecuteAsync(JobExecutionContext context)
    {
        var rolloverStrategy = context.ServiceProvider.GetRequiredService<IIndexRolloverStrategy>();
        await rolloverStrategy.ApplyAsync(Utils.GetElasticDocumentTypes(), context.CancellationToken);
    }
}