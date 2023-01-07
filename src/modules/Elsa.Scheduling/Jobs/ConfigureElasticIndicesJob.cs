using System.Text.Json.Serialization;
using Elsa.Elasticsearch.Common;
using Elsa.Jobs.Models;
using Nest;
using Job = Elsa.Jobs.Abstractions.Job;

namespace Elsa.Scheduling.Jobs;

public class ConfigureElasticIndicesJob : Job
{
    [JsonConstructor]
    public ConfigureElasticIndicesJob()
    {
    }

    protected override async ValueTask ExecuteAsync(JobExecutionContext context)
    {
        var client = context.GetRequiredService<ElasticClient>();
        var indexAliasGroups = await client.Indices.GetAliasAsync();

        foreach (var group in indexAliasGroups.Indices)
        {
            // Only 1 alias exists per index in Elsa Elasticsearch configuration
            var aliasPointingCurrentIndex = group.Value.Aliases.Keys.Single();
            
            var currentIndexName = group.Key.Name;
            var newIndexName = Utils.GenerateIndexName(aliasPointingCurrentIndex);
                
            var indexExists = (await client.Indices.ExistsAsync(newIndexName)).Exists;
            if (indexExists) continue;
            
            // Point the alias to the new index
            await client.Indices.BulkAliasAsync(aliases => aliases
                .Remove(a => a.Alias(aliasPointingCurrentIndex).Index(currentIndexName))
                .Add(a => a.Alias(aliasPointingCurrentIndex).Index(newIndexName)));
        }
    }
}