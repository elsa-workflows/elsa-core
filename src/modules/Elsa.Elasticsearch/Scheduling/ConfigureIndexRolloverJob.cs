using Elsa.Elasticsearch.Services;
using Elsa.Jobs.Models;
using Microsoft.Extensions.DependencyInjection;
using Job = Elsa.Jobs.Abstractions.Job;

namespace Elsa.Elasticsearch.Scheduling;

/// <summary>
/// A job that applies the <see cref="IIndexRolloverStrategy"/>
/// </summary>
public class ConfigureIndexRolloverJob : Job
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public ConfigureIndexRolloverJob()
    {
    }

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(JobExecutionContext context)
    {
        var rolloverStrategy = context.ServiceProvider.GetRequiredService<IIndexRolloverStrategy>();
        await rolloverStrategy.ApplyAsync(context.CancellationToken);
    }
}